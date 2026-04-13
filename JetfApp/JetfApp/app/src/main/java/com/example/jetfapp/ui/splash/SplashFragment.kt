package com.example.jetfapp.ui.splash

import android.app.DownloadManager
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.os.Environment
import android.provider.Settings
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.fragment.app.activityViewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import com.example.jetfapp.BuildConfig
import com.example.jetfapp.MainActivity
import com.example.jetfapp.R
import com.example.jetfapp.databinding.FragmentSplashBinding
import com.example.jetfapp.viewmodel.AppEvent
import com.example.jetfapp.viewmodel.AppViewModel
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import java.net.URI
import kotlinx.coroutines.launch

class SplashFragment : Fragment() {
    private companion object {
        const val UPDATE_PREFS_NAME = "app_update"
        const val KEY_PENDING_DOWNLOAD_ID = "pending_download_id"
        const val KEY_PENDING_VERSION = "pending_version"
        const val KEY_AWAITING_INSTALL_PERMISSION = "awaiting_install_permission"
    }

    private var _binding: FragmentSplashBinding? = null
    private var apkDownloadId: Long? = null
    private val binding: FragmentSplashBinding
        get() = checkNotNull(_binding)

    private val appViewModel: AppViewModel by activityViewModels { AppViewModel.factory() }
    private val updatePreferences by lazy {
        requireContext().getSharedPreferences(UPDATE_PREFS_NAME, Context.MODE_PRIVATE)
    }

    private val apkDownloadReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context?, intent: Intent?) {
            val completedDownloadId = intent?.getLongExtra(DownloadManager.EXTRA_DOWNLOAD_ID, -1L) ?: -1L
            if (completedDownloadId == -1L || completedDownloadId != apkDownloadId) {
                return
            }

            handleApkDownloadCompleted(completedDownloadId)
        }
    }

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentSplashBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        binding.textVersion.text = getString(R.string.label_version_short, BuildConfig.VERSION_NAME)
        cleanupDownloadedApkIfInstalled()
        binding.textStatus.setOnClickListener {
            if (appViewModel.splashState.value.canRetry) {
                appViewModel.retryVersionCheck()
            }
        }

        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch {
                    appViewModel.splashState.collect { state ->
                        binding.textStatus.text = state.statusMessage
                        binding.progressLoading.isIndeterminate = state.isLoading
                        binding.progressLoading.visibility = if (state.isLoading) View.VISIBLE else View.INVISIBLE
                    }
                }

                launch {
                    appViewModel.events.collect { event ->
                        when (event) {
                            AppEvent.NavigateToLogin -> (activity as? MainActivity)?.showLogin()
                            is AppEvent.ShowUpdatePrompt -> showUpdateDialog(event)
                            AppEvent.NavigateToMenu -> Unit
                        }
                    }
                }
            }
        }

        appViewModel.startVersionCheck()
    }

    override fun onResume() {
        super.onResume()
        resumeInstallAfterPermissionGrantIfNeeded()
    }

    override fun onStart() {
        super.onStart()
        val intentFilter = IntentFilter(DownloadManager.ACTION_DOWNLOAD_COMPLETE)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            requireContext().registerReceiver(apkDownloadReceiver, intentFilter, Context.RECEIVER_NOT_EXPORTED)
        } else {
            @Suppress("DEPRECATION")
            requireContext().registerReceiver(apkDownloadReceiver, intentFilter)
        }
    }

    override fun onStop() {
        runCatching {
            requireContext().unregisterReceiver(apkDownloadReceiver)
        }
        super.onStop()
    }

    private fun showUpdateDialog(event: AppEvent.ShowUpdatePrompt) {
        val dialogBuilder = MaterialAlertDialogBuilder(requireContext())
            .setTitle(getString(R.string.label_version, event.latestVersion))
            .setMessage(event.message)
            .setCancelable(!event.forceUpdate)
            .setPositiveButton(R.string.action_update) { _, _ ->
                val resolvedApkUrl = resolveApkDownloadUrl(event.apkUrl)
                if (resolvedApkUrl.isNotBlank()) {
                    downloadAndInstallApk(resolvedApkUrl, event.latestVersion)
                } else {
                    binding.textStatus.text = getString(R.string.message_update_not_available)
                }
            }

        if (event.forceUpdate) {
            dialogBuilder.setNegativeButton(R.string.action_cancel, null)
        } else {
            dialogBuilder.setNegativeButton(R.string.action_continue) { _, _ ->
                appViewModel.continueWithCurrentVersion()
            }
        }

        dialogBuilder.show()
    }

    private fun downloadAndInstallApk(apkUrl: String, latestVersion: String) {
        val downloadManager = requireContext().getSystemService(DownloadManager::class.java)
        val request = DownloadManager.Request(Uri.parse(apkUrl))
            .setTitle(getString(R.string.app_name))
            .setDescription(getString(R.string.message_update_downloading, latestVersion))
            .setMimeType("application/vnd.android.package-archive")
            .setNotificationVisibility(DownloadManager.Request.VISIBILITY_VISIBLE_NOTIFY_COMPLETED)
            .setAllowedOverMetered(true)
            .setAllowedOverRoaming(true)
            .setDestinationInExternalFilesDir(
                requireContext(),
                Environment.DIRECTORY_DOWNLOADS,
                "JETFApp-${latestVersion}.apk"
            )

        apkDownloadId = downloadManager.enqueue(request)
        persistPendingUpdate(apkDownloadId = checkNotNull(apkDownloadId), targetVersion = latestVersion)
        binding.textStatus.text = getString(R.string.message_update_downloading, latestVersion)
    }

    private fun handleApkDownloadCompleted(downloadId: Long) {
        val downloadManager = requireContext().getSystemService(DownloadManager::class.java)
        val query = DownloadManager.Query().setFilterById(downloadId)
        downloadManager.query(query).use { cursor ->
            if (!cursor.moveToFirst()) {
                binding.textStatus.text = getString(R.string.message_update_download_failed)
                return
            }

            val status = cursor.getInt(cursor.getColumnIndexOrThrow(DownloadManager.COLUMN_STATUS))
            if (status != DownloadManager.STATUS_SUCCESSFUL) {
                val reason = cursor.getInt(cursor.getColumnIndexOrThrow(DownloadManager.COLUMN_REASON))
                binding.textStatus.text = getString(
                    R.string.message_update_download_failed_with_reason,
                    describeDownloadFailureReason(reason)
                )
                return
            }
        }

        val apkUri = downloadManager.getUriForDownloadedFile(downloadId)
        if (apkUri == null) {
            binding.textStatus.text = getString(R.string.message_update_download_failed)
            return
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O && !requireContext().packageManager.canRequestPackageInstalls()) {
            updatePreferences.edit().putBoolean(KEY_AWAITING_INSTALL_PERMISSION, true).apply()
            binding.textStatus.text = getString(R.string.message_enable_install_permission)
            startActivity(
                Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES).apply {
                    data = Uri.parse("package:${requireContext().packageName}")
                }
            )
            return
        }

        launchInstaller(apkUri)
    }

    private fun resumeInstallAfterPermissionGrantIfNeeded() {
        if (!updatePreferences.getBoolean(KEY_AWAITING_INSTALL_PERMISSION, false)) {
            return
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O && !requireContext().packageManager.canRequestPackageInstalls()) {
            return
        }

        updatePreferences.edit().putBoolean(KEY_AWAITING_INSTALL_PERMISSION, false).apply()
        val pendingDownloadId = updatePreferences.getLong(KEY_PENDING_DOWNLOAD_ID, -1L)
        if (pendingDownloadId == -1L) {
            return
        }

        val downloadManager = requireContext().getSystemService(DownloadManager::class.java)
        val apkUri = downloadManager.getUriForDownloadedFile(pendingDownloadId)
        if (apkUri != null) {
            launchInstaller(apkUri)
        }
    }

    private fun launchInstaller(apkUri: Uri) {
        binding.textStatus.text = getString(R.string.message_update_ready_to_install)
        startActivity(
            Intent(Intent.ACTION_VIEW).apply {
                setDataAndType(apkUri, "application/vnd.android.package-archive")
                addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
                addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
            }
        )
    }

    private fun resolveApkDownloadUrl(apkUrl: String): String {
        val fallbackUrl = runCatching {
            URI(BuildConfig.API_BASE_URL).resolve("api/app/download-apk").toString()
        }.getOrDefault("")

        if (apkUrl.isBlank()) {
            return fallbackUrl
        }

        return runCatching {
            val configuredApiBaseUri = URI(BuildConfig.API_BASE_URL)
            val downloadUri = URI(apkUrl)
            if (!downloadUri.isAbsolute) {
                configuredApiBaseUri.resolve(downloadUri).toString()
            } else {
                val sameHost = downloadUri.host.equals(configuredApiBaseUri.host, ignoreCase = true)
                if (sameHost) {
                    URI(
                        configuredApiBaseUri.scheme,
                        configuredApiBaseUri.userInfo,
                        configuredApiBaseUri.host,
                        configuredApiBaseUri.port,
                        downloadUri.path,
                        downloadUri.query,
                        downloadUri.fragment
                    ).toString()
                } else {
                    val expectedPathBase = configuredApiBaseUri.path.removeSuffix("/")
                    if (expectedPathBase.isNotEmpty() && !downloadUri.path.startsWith(expectedPathBase)) {
                        fallbackUrl.ifBlank { apkUrl }
                    } else {
                        apkUrl
                    }
                }
            }
        }.getOrDefault(fallbackUrl.ifBlank { apkUrl })
    }

    private fun cleanupDownloadedApkIfInstalled() {
        val targetVersion = updatePreferences.getString(KEY_PENDING_VERSION, null) ?: return
        if (targetVersion != BuildConfig.VERSION_NAME) {
            return
        }

        val pendingDownloadId = updatePreferences.getLong(KEY_PENDING_DOWNLOAD_ID, -1L)
        if (pendingDownloadId != -1L) {
            val downloadManager = requireContext().getSystemService(DownloadManager::class.java)
            downloadManager.remove(pendingDownloadId)
        }

        clearPendingUpdate()
    }

    private fun persistPendingUpdate(apkDownloadId: Long, targetVersion: String) {
        updatePreferences.edit()
            .putLong(KEY_PENDING_DOWNLOAD_ID, apkDownloadId)
            .putString(KEY_PENDING_VERSION, targetVersion)
            .putBoolean(KEY_AWAITING_INSTALL_PERMISSION, false)
            .apply()
    }

    private fun clearPendingUpdate() {
        updatePreferences.edit()
            .remove(KEY_PENDING_DOWNLOAD_ID)
            .remove(KEY_PENDING_VERSION)
            .remove(KEY_AWAITING_INSTALL_PERMISSION)
            .apply()
    }

    private fun describeDownloadFailureReason(reason: Int): String {
        return when (reason) {
            DownloadManager.ERROR_CANNOT_RESUME -> getString(R.string.message_update_error_cannot_resume)
            DownloadManager.ERROR_DEVICE_NOT_FOUND -> getString(R.string.message_update_error_device_not_found)
            DownloadManager.ERROR_FILE_ALREADY_EXISTS -> getString(R.string.message_update_error_file_exists)
            DownloadManager.ERROR_FILE_ERROR -> getString(R.string.message_update_error_file)
            DownloadManager.ERROR_HTTP_DATA_ERROR -> getString(R.string.message_update_error_http)
            DownloadManager.ERROR_INSUFFICIENT_SPACE -> getString(R.string.message_update_error_no_space)
            DownloadManager.ERROR_TOO_MANY_REDIRECTS -> getString(R.string.message_update_error_redirect)
            DownloadManager.ERROR_UNHANDLED_HTTP_CODE,
            DownloadManager.ERROR_UNKNOWN -> getString(R.string.message_update_error_unknown)
            404 -> getString(R.string.message_update_error_not_found)
            else -> getString(R.string.message_update_error_code, reason)
        }
    }

    override fun onDestroyView() {
        _binding = null
        super.onDestroyView()
    }
}