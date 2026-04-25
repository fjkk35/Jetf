package com.example.jetfapp.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.example.jetfapp.data.model.ApiResult
import com.example.jetfapp.data.model.BatchUpdateLocationCodeRequest
import com.example.jetfapp.data.model.GetBatchLocationUpdateCountRequest
import com.example.jetfapp.data.repository.ShipmentInboundRepository
import com.example.jetfapp.di.ServiceLocator
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class BatchLocationTransferUiState(
    val oldLocationCode: String = "",
    val newLocationCode: String = "",
    val isSubmitting: Boolean = false,
    val message: String? = null
)

sealed interface BatchLocationTransferEvent {
    data object NavigateBack : BatchLocationTransferEvent
    data object FocusOldLocation : BatchLocationTransferEvent
    data object FocusNewLocation : BatchLocationTransferEvent
    data class ShowConfirmationDialog(val message: String, val canConfirm: Boolean) : BatchLocationTransferEvent
}

class BatchLocationTransferViewModel(
    private val repository: ShipmentInboundRepository
) : ViewModel() {
    private val _uiState = MutableStateFlow(BatchLocationTransferUiState())
    val uiState = _uiState.asStateFlow()

    private val _events = MutableSharedFlow<BatchLocationTransferEvent>(extraBufferCapacity = 1)
    val events = _events.asSharedFlow()

    private var editUser: String = ""

    fun updateEditUser(account: String) {
        editUser = account.trim()
    }

    fun updateOldLocationCode(locationCode: String) {
        _uiState.update {
            it.copy(oldLocationCode = normalizeLocationCode(locationCode), message = null)
        }
    }

    fun updateNewLocationCode(locationCode: String) {
        _uiState.update {
            it.copy(newLocationCode = normalizeLocationCode(locationCode), message = null)
        }
    }

    fun requestPreview(oldLocationInput: String? = null, newLocationInput: String? = null) {
        val oldLocationCode = normalizeLocationCode(oldLocationInput ?: _uiState.value.oldLocationCode)
        val newLocationCode = normalizeLocationCode(newLocationInput ?: _uiState.value.newLocationCode)

        if (!validateLocationInputs(oldLocationCode, newLocationCode)) {
            return
        }

        _uiState.update {
            it.copy(
                oldLocationCode = oldLocationCode,
                newLocationCode = newLocationCode,
                isSubmitting = true,
                message = null
            )
        }

        viewModelScope.launch {
            when (
                val result = repository.getBatchLocationUpdateCount(
                    GetBatchLocationUpdateCountRequest(
                        oldLocationCode = oldLocationCode,
                        newLocationCode = newLocationCode
                    )
                )
            ) {
                is ApiResult.Success -> {
                    _uiState.update { it.copy(isSubmitting = false, message = null) }
                    _events.emit(BatchLocationTransferEvent.ShowConfirmationDialog(result.message, true))
                }

                is ApiResult.Failure -> {
                    _uiState.update { it.copy(isSubmitting = false, message = result.message) }
                    _events.emit(BatchLocationTransferEvent.ShowConfirmationDialog(result.message, false))
                }
            }
        }
    }

    fun confirmBatchUpdate() {
        val currentState = _uiState.value
        val oldLocationCode = normalizeLocationCode(currentState.oldLocationCode)
        val newLocationCode = normalizeLocationCode(currentState.newLocationCode)

        if (!validateLocationInputs(oldLocationCode, newLocationCode)) {
            return
        }

        if (editUser.isBlank()) {
            _uiState.update { it.copy(message = "請先登入帳號。") }
            return
        }

        _uiState.update {
            it.copy(isSubmitting = true, message = null)
        }

        viewModelScope.launch {
            when (
                val result = repository.batchUpdateLocationCode(
                    BatchUpdateLocationCodeRequest(
                        oldLocationCode = oldLocationCode,
                        newLocationCode = newLocationCode,
                        editUser = editUser
                    )
                )
            ) {
                is ApiResult.Success -> {
                    _uiState.value = BatchLocationTransferUiState(message = result.message)
                    _events.emit(BatchLocationTransferEvent.FocusOldLocation)
                }

                is ApiResult.Failure -> {
                    _uiState.update { it.copy(isSubmitting = false, message = result.message) }
                }
            }
        }
    }

    fun returnToMenu() {
        _events.tryEmit(BatchLocationTransferEvent.NavigateBack)
    }

    private fun validateLocationInputs(oldLocationCode: String, newLocationCode: String): Boolean {
        when {
            oldLocationCode.isBlank() -> {
                _uiState.update {
                    it.copy(oldLocationCode = oldLocationCode, newLocationCode = newLocationCode, message = "請輸入原儲位。")
                }
                _events.tryEmit(BatchLocationTransferEvent.FocusOldLocation)
                return false
            }

            newLocationCode.isBlank() -> {
                _uiState.update {
                    it.copy(oldLocationCode = oldLocationCode, newLocationCode = newLocationCode, message = "請輸入新儲位。")
                }
                _events.tryEmit(BatchLocationTransferEvent.FocusNewLocation)
                return false
            }

            oldLocationCode == newLocationCode -> {
                _uiState.update {
                    it.copy(oldLocationCode = oldLocationCode, newLocationCode = newLocationCode, message = "原儲位不可與新儲位相同")
                }
                _events.tryEmit(BatchLocationTransferEvent.FocusNewLocation)
                return false
            }
        }

        return true
    }

    private fun normalizeLocationCode(locationCode: String): String {
        return locationCode.trim().uppercase()
    }

    companion object {
        fun factory(): ViewModelProvider.Factory {
            return object : ViewModelProvider.Factory {
                @Suppress("UNCHECKED_CAST")
                override fun <T : ViewModel> create(modelClass: Class<T>): T {
                    if (modelClass.isAssignableFrom(BatchLocationTransferViewModel::class.java)) {
                        return BatchLocationTransferViewModel(ServiceLocator.provideShipmentInboundRepository()) as T
                    }
                    throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
                }
            }
        }
    }
}
