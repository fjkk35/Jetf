package com.example.jetfapp

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.os.Build
import android.os.Bundle
import android.os.SystemClock
import android.view.KeyEvent
import android.view.inputmethod.InputMethodManager
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.isVisible
import androidx.core.view.ViewCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.updateLayoutParams
import androidx.fragment.app.Fragment
import com.example.jetfapp.databinding.ActivityMainBinding
import com.example.jetfapp.di.ServiceLocator
import com.example.jetfapp.ui.common.FunctionKey
import com.example.jetfapp.ui.common.FunctionKeyHandler
import com.example.jetfapp.ui.common.KeyboardWedgeScanHandler
import com.example.jetfapp.ui.common.ScanInputHandler
import com.example.jetfapp.ui.inbound.InboundSettingsFragment
import com.example.jetfapp.ui.inbound.InboundWorkFragment
import com.example.jetfapp.ui.login.LoginFragment
import com.example.jetfapp.ui.menu.MenuFragment
import com.example.jetfapp.ui.splash.SplashFragment

class MainActivity : AppCompatActivity() {
    private companion object {
        const val scannerTriggerGracePeriodMs = 750L
        val scannerTriggerKeyCodes = setOf(
            KeyEvent.KEYCODE_F9,
            KeyEvent.KEYCODE_F10,
            KeyEvent.KEYCODE_F11,
            KeyEvent.KEYCODE_BUTTON_L1,
            KeyEvent.KEYCODE_BUTTON_R1
        )
    }

    private lateinit var binding: ActivityMainBinding

    private val appConfig = ServiceLocator.provideAppConfig()
    private var isBottomActionBarRequested = false
    private var isBottomActionBarSuppressed = false
    private val wedgeScanBuffer = StringBuilder()
    private var scannerTriggerTimestampMs = 0L

    private val scanReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context?, intent: Intent?) {
            val scanValue = extractScanValue(intent) ?: return
            (currentFragment() as? ScanInputHandler)?.onScanReceived(scanValue)
        }
    }

    private var isScanReceiverRegistered = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.textAppVersion.text = getString(R.string.label_version_short, BuildConfig.VERSION_NAME)
        ViewCompat.setOnApplyWindowInsetsListener(binding.main) { _, windowInsets ->
            val systemBarsInsets = windowInsets.getInsets(WindowInsetsCompat.Type.systemBars())
            binding.textAppVersion.updateLayoutParams<androidx.constraintlayout.widget.ConstraintLayout.LayoutParams> {
                topMargin = systemBarsInsets.top + resources.getDimensionPixelSize(R.dimen.version_label_top_spacing)
                marginEnd = resources.getDimensionPixelSize(R.dimen.version_label_end_spacing)
            }
            windowInsets
        }

        binding.buttonF3.setOnClickListener {
            dispatchFunctionKey(FunctionKey.F3)
        }
        binding.buttonF4.setOnClickListener {
            dispatchFunctionKey(FunctionKey.F4)
        }

        supportFragmentManager.addOnBackStackChangedListener {
            updateBottomActionBar()
        }

        if (savedInstanceState == null) {
            showSplash()
        } else {
            updateBottomActionBar()
        }
    }

    override fun onResume() {
        super.onResume()
        registerScanReceiverIfNeeded()
    }

    override fun onPause() {
        unregisterScanReceiverIfNeeded()
        super.onPause()
    }

    override fun dispatchKeyEvent(event: KeyEvent): Boolean {
        if (event.action == KeyEvent.ACTION_UP) {
            when (event.keyCode) {
                KeyEvent.KEYCODE_F3 -> {
                    if (currentFragment() is InboundSettingsFragment || currentFragment() is InboundWorkFragment) {
                        dispatchFunctionKey(FunctionKey.F3)
                        return true
                    }
                }
                KeyEvent.KEYCODE_F4 -> {
                    if (currentFragment() is InboundSettingsFragment || currentFragment() is InboundWorkFragment) {
                        dispatchFunctionKey(FunctionKey.F4)
                        return true
                    }
                }
            }
        }

        val wedgeHandler = currentFragment() as? KeyboardWedgeScanHandler
        if (wedgeHandler?.shouldConsumeWedgeInput() == true) {
            if (event.keyCode in scannerTriggerKeyCodes) {
                if (event.action == KeyEvent.ACTION_DOWN) {
                    wedgeScanBuffer.clear()
                }
                scannerTriggerTimestampMs = SystemClock.elapsedRealtime()
                return true
            }

            if (event.action == KeyEvent.ACTION_DOWN) {
                val shouldConsumeWedgeCharacters = isScannerWedgeInputActive()
                when (event.keyCode) {
                    KeyEvent.KEYCODE_ENTER,
                    KeyEvent.KEYCODE_NUMPAD_ENTER,
                    KeyEvent.KEYCODE_TAB -> {
                        if (shouldConsumeWedgeCharacters) {
                            val scannedValue = wedgeScanBuffer.toString().trim()
                            wedgeScanBuffer.clear()
                            if (scannedValue.isNotEmpty()) {
                                wedgeHandler.onScanReceived(scannedValue)
                            }
                            return true
                        }
                    }
                    else -> {
                        if (shouldConsumeWedgeCharacters) {
                            val unicodeChar = event.unicodeChar
                            if (unicodeChar != 0) {
                                val character = unicodeChar.toChar()
                                if (!character.isISOControl()) {
                                    wedgeScanBuffer.append(character)
                                    return true
                                }
                            }
                        }
                    }
                }
            }
        }

        if (event.action == KeyEvent.ACTION_UP && currentFragment() is MenuFragment) {
            if (event.keyCode == KeyEvent.KEYCODE_1 || event.keyCode == KeyEvent.KEYCODE_NUMPAD_1) {
                hideKeyboardAndClearFocus()
                showInboundSettings()
                return true
            }
        }

        return super.dispatchKeyEvent(event)
    }

    fun showSplash() {
        navigateTo(SplashFragment(), clearBackStack = true, addToBackStack = false)
    }

    fun showLogin() {
        navigateTo(LoginFragment(), clearBackStack = true, addToBackStack = false)
    }

    fun showMenu() {
        navigateTo(MenuFragment(), clearBackStack = true, addToBackStack = false)
    }

    fun showInboundSettings() {
        navigateTo(InboundSettingsFragment(), clearBackStack = false, addToBackStack = true)
    }

    fun showInboundWork() {
        navigateTo(InboundWorkFragment(), clearBackStack = false, addToBackStack = true)
    }

    fun setBottomActionBarSuppressed(suppressed: Boolean) {
        if (isBottomActionBarSuppressed == suppressed) {
            return
        }

        isBottomActionBarSuppressed = suppressed
        renderBottomActionBar()
    }

    private fun navigateTo(fragment: Fragment, clearBackStack: Boolean, addToBackStack: Boolean) {
        hideKeyboardAndClearFocus()

        if (clearBackStack) {
            supportFragmentManager.popBackStack(null, androidx.fragment.app.FragmentManager.POP_BACK_STACK_INCLUSIVE)
        }

        supportFragmentManager.beginTransaction().apply {
            replace(R.id.fragment_container, fragment)
            if (addToBackStack) {
                addToBackStack(fragment::class.java.simpleName)
            }
        }.commit()

        supportFragmentManager.executePendingTransactions()
        updateBottomActionBar()
    }

    private fun dispatchFunctionKey(functionKey: FunctionKey) {
        (currentFragment() as? FunctionKeyHandler)?.onFunctionKeyPressed(functionKey)
    }

    private fun currentFragment(): Fragment? {
        return supportFragmentManager.findFragmentById(R.id.fragment_container)
    }

    private fun updateBottomActionBar() {
        when (currentFragment()) {
            is SplashFragment -> showBottomActionBar(false)
            is LoginFragment -> showBottomActionBar(false)
            is MenuFragment -> showBottomActionBar(false)
            is InboundSettingsFragment -> showBottomActionBar(true, getString(R.string.label_function_f3, getString(R.string.action_back)), getString(R.string.label_function_f4, getString(R.string.action_next)))
            is InboundWorkFragment -> showBottomActionBar(true, getString(R.string.label_function_f3, getString(R.string.action_back)), getString(R.string.label_function_f4, getString(R.string.action_change_location)))
            else -> showBottomActionBar(false)
        }
    }

    private fun showBottomActionBar(visible: Boolean, f3Text: String = "", f4Text: String = "") {
        isBottomActionBarRequested = visible
        binding.buttonF3.text = f3Text
        binding.buttonF4.text = f4Text
        renderBottomActionBar()
    }

    private fun renderBottomActionBar() {
        val visible = isBottomActionBarRequested && !isBottomActionBarSuppressed
        binding.bottomActionBar.isVisible = visible
        binding.buttonF3.isVisible = visible
        binding.buttonF4.isVisible = visible
        binding.textAppVersion.isVisible = currentFragment() !is SplashFragment
    }

    private fun hideKeyboardAndClearFocus() {
        wedgeScanBuffer.clear()
        currentFocus?.let { focusedView ->
            val inputMethodManager = getSystemService(Context.INPUT_METHOD_SERVICE) as? InputMethodManager
            inputMethodManager?.hideSoftInputFromWindow(focusedView.windowToken, 0)
            focusedView.clearFocus()
        }
        setBottomActionBarSuppressed(false)
    }

    private fun isScannerWedgeInputActive(): Boolean {
        if (wedgeScanBuffer.isNotEmpty()) {
            return true
        }

        val elapsedSinceTrigger = SystemClock.elapsedRealtime() - scannerTriggerTimestampMs
        return elapsedSinceTrigger in 0..scannerTriggerGracePeriodMs
    }

    private fun registerScanReceiverIfNeeded() {
        if (isScanReceiverRegistered) {
            return
        }

        val intentFilter = IntentFilter(appConfig.scanIntentAction)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            registerReceiver(scanReceiver, intentFilter, Context.RECEIVER_NOT_EXPORTED)
        } else {
            @Suppress("DEPRECATION")
            registerReceiver(scanReceiver, intentFilter)
        }
        isScanReceiverRegistered = true
    }

    private fun unregisterScanReceiverIfNeeded() {
        if (!isScanReceiverRegistered) {
            return
        }

        runCatching {
            unregisterReceiver(scanReceiver)
        }
        isScanReceiverRegistered = false
    }

    private fun extractScanValue(intent: Intent?): String? {
        val extras = intent?.extras ?: return null
        appConfig.scanDataKeys.forEach { key ->
            val value = extras.getString(key)
            if (!value.isNullOrBlank()) {
                return value.trim()
            }
        }
        return null
    }
}