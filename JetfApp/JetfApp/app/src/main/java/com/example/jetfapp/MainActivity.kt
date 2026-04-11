package com.example.jetfapp

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.os.Build
import android.os.Bundle
import android.view.KeyEvent
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.isVisible
import androidx.fragment.app.Fragment
import com.example.jetfapp.databinding.ActivityMainBinding
import com.example.jetfapp.di.ServiceLocator
import com.example.jetfapp.ui.common.FunctionKey
import com.example.jetfapp.ui.common.FunctionKeyHandler
import com.example.jetfapp.ui.common.ScanInputHandler
import com.example.jetfapp.ui.inbound.InboundSettingsFragment
import com.example.jetfapp.ui.inbound.InboundWorkFragment
import com.example.jetfapp.ui.login.LoginFragment
import com.example.jetfapp.ui.menu.MenuFragment
import com.example.jetfapp.ui.splash.SplashFragment

class MainActivity : AppCompatActivity() {
    private lateinit var binding: ActivityMainBinding

    private val appConfig = ServiceLocator.provideAppConfig()

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

    override fun onKeyDown(keyCode: Int, event: KeyEvent?): Boolean {
        return when (keyCode) {
            KeyEvent.KEYCODE_F3 -> {
                dispatchFunctionKey(FunctionKey.F3)
                true
            }

            KeyEvent.KEYCODE_F4 -> {
                dispatchFunctionKey(FunctionKey.F4)
                true
            }

            else -> super.onKeyDown(keyCode, event)
        }
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

    private fun navigateTo(fragment: Fragment, clearBackStack: Boolean, addToBackStack: Boolean) {
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
            is LoginFragment -> showBottomActionBar(true, getString(R.string.label_function_f3, getString(R.string.action_exit)), getString(R.string.label_function_f4, getString(R.string.action_login)))
            is MenuFragment -> showBottomActionBar(true, getString(R.string.label_function_f3, getString(R.string.action_exit)), getString(R.string.label_function_f4, getString(R.string.action_confirm)))
            is InboundSettingsFragment -> showBottomActionBar(true, getString(R.string.label_function_f3, getString(R.string.action_exit)), getString(R.string.label_function_f4, getString(R.string.action_next)))
            is InboundWorkFragment -> showBottomActionBar(true, getString(R.string.label_function_f3, getString(R.string.action_back)), getString(R.string.label_function_f4, getString(R.string.action_change_location)))
            else -> showBottomActionBar(false)
        }
    }

    private fun showBottomActionBar(visible: Boolean, f3Text: String = "", f4Text: String = "") {
        binding.bottomActionBar.isVisible = visible
        binding.buttonF3.isVisible = visible
        binding.buttonF4.isVisible = visible
        binding.buttonF3.text = f3Text
        binding.buttonF4.text = f4Text
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