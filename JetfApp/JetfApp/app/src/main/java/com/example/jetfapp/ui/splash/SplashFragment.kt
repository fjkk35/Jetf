package com.example.jetfapp.ui.splash

import android.content.Intent
import android.net.Uri
import android.os.Bundle
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
import kotlinx.coroutines.launch

class SplashFragment : Fragment() {
    private var _binding: FragmentSplashBinding? = null
    private val binding: FragmentSplashBinding
        get() = checkNotNull(_binding)

    private val appViewModel: AppViewModel by activityViewModels { AppViewModel.factory() }

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
        binding.textVersion.text = getString(R.string.label_version, BuildConfig.VERSION_NAME)
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

    private fun showUpdateDialog(event: AppEvent.ShowUpdatePrompt) {
        val dialogBuilder = MaterialAlertDialogBuilder(requireContext())
            .setTitle(getString(R.string.label_version, event.latestVersion))
            .setMessage(buildString {
                append(event.message)
                if (event.apkUrl.isNotBlank()) {
                    append("\n\n")
                    append(event.apkUrl)
                }
            })
            .setCancelable(!event.forceUpdate)
            .setPositiveButton(R.string.action_update) { _, _ ->
                if (event.apkUrl.isNotBlank()) {
                    startActivity(Intent(Intent.ACTION_VIEW, Uri.parse(event.apkUrl)))
                }
                if (!event.forceUpdate) {
                    appViewModel.continueWithCurrentVersion()
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

    override fun onDestroyView() {
        _binding = null
        super.onDestroyView()
    }
}