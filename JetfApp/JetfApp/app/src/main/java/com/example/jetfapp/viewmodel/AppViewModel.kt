package com.example.jetfapp.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.example.jetfapp.BuildConfig
import com.example.jetfapp.data.model.ApiResult
import com.example.jetfapp.data.model.VersionCheckData
import com.example.jetfapp.data.repository.AppRepository
import com.example.jetfapp.di.ServiceLocator
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class SplashUiState(
    val versionLabel: String = BuildConfig.VERSION_NAME,
    val statusMessage: String = "系統啟動中，請稍候…",
    val isLoading: Boolean = true,
    val canRetry: Boolean = false
)

data class LoginUiState(
    val account: String = "",
    val isSubmitting: Boolean = false,
    val message: String? = null
)

sealed interface AppEvent {
    data object NavigateToLogin : AppEvent
    data object NavigateToMenu : AppEvent
    data class ShowUpdatePrompt(
        val forceUpdate: Boolean,
        val latestVersion: String,
        val apkUrl: String,
        val message: String
    ) : AppEvent
}

class AppViewModel(
    private val repository: AppRepository
) : ViewModel() {
    private val _splashState = MutableStateFlow(SplashUiState())
    val splashState = _splashState.asStateFlow()

    private val _loginState = MutableStateFlow(LoginUiState())
    val loginState = _loginState.asStateFlow()

    private val _currentAccount = MutableStateFlow<String?>(null)
    val currentAccount = _currentAccount.asStateFlow()

    private val _events = MutableSharedFlow<AppEvent>(extraBufferCapacity = 1)
    val events = _events.asSharedFlow()

    private var hasCheckedVersion = false

    fun startVersionCheck() {
        if (hasCheckedVersion) {
            return
        }
        hasCheckedVersion = true

        viewModelScope.launch {
            _splashState.update {
                it.copy(isLoading = true, canRetry = false, statusMessage = "系統啟動中，請稍候…")
            }

            when (val result = repository.checkVersion(BuildConfig.VERSION_NAME)) {
                is ApiResult.Success -> handleVersionSuccess(result.data)
                is ApiResult.Failure -> {
                    hasCheckedVersion = false
                    _splashState.update {
                        it.copy(
                            isLoading = false,
                            canRetry = true,
                            statusMessage = result.message
                        )
                    }
                }
            }
        }
    }

    fun retryVersionCheck() {
        hasCheckedVersion = false
        startVersionCheck()
    }

    fun continueWithCurrentVersion() {
        _events.tryEmit(AppEvent.NavigateToLogin)
    }

    fun updateAccount(account: String) {
        _loginState.update { it.copy(account = account, message = null) }
    }

    fun login() {
        val account = _loginState.value.account.trim()
        if (account.isBlank()) {
            _loginState.update { it.copy(message = "請輸入帳號。") }
            return
        }

        viewModelScope.launch {
            _loginState.update { it.copy(isSubmitting = true, message = null) }

            when (val result = repository.login(account = account, versionCode = BuildConfig.VERSION_NAME)) {
                is ApiResult.Success -> {
                    if (result.data) {
                        _currentAccount.value = account
                        _loginState.update { it.copy(isSubmitting = false, message = null) }
                        _events.emit(AppEvent.NavigateToMenu)
                    } else {
                        _loginState.update {
                            it.copy(
                                isSubmitting = false,
                                message = "登入失敗，請確認帳號是否正確。"
                            )
                        }
                    }
                }

                is ApiResult.Failure -> {
                    _loginState.update {
                        it.copy(
                            isSubmitting = false,
                            message = result.message
                        )
                    }
                }
            }
        }
    }

    private fun handleVersionSuccess(data: VersionCheckData) {
        _splashState.update {
            it.copy(isLoading = false, canRetry = false, statusMessage = data.message)
        }

        if (data.latestVersionCode == BuildConfig.VERSION_NAME) {
            _events.tryEmit(AppEvent.NavigateToLogin)
            return
        }

        _events.tryEmit(
            AppEvent.ShowUpdatePrompt(
                forceUpdate = data.forceUpdate,
                latestVersion = data.latestVersionCode,
                apkUrl = data.apkUrl,
                message = data.message
            )
        )
    }

    companion object {
        fun factory(): ViewModelProvider.Factory {
            return object : ViewModelProvider.Factory {
                @Suppress("UNCHECKED_CAST")
                override fun <T : ViewModel> create(modelClass: Class<T>): T {
                    if (modelClass.isAssignableFrom(AppViewModel::class.java)) {
                        return AppViewModel(ServiceLocator.provideAppRepository()) as T
                    }
                    throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
                }
            }
        }
    }
}