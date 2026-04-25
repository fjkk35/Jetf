package com.example.jetfapp.viewmodel

import android.graphics.Bitmap
import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.example.jetfapp.data.model.ApiResult
import com.example.jetfapp.data.model.ShipmentInboundExceptionRequest
import com.example.jetfapp.data.repository.ShipmentInboundRepository
import com.example.jetfapp.di.ServiceLocator
import com.example.jetfapp.utils.SequenceNumberUtil
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class ShipmentExceptionUiState(
    val seqNo: String = "",
    val reason: String = "",
    val photoPreview: Bitmap? = null,
    val photoBase64: String? = null,
    val isSubmitting: Boolean = false,
    val message: String? = null
)

sealed interface ShipmentExceptionEvent {
    data object NavigateToMenu : ShipmentExceptionEvent
    data class ShowUploadResultDialog(val message: String, val isSuccess: Boolean) : ShipmentExceptionEvent
}

class ShipmentExceptionViewModel(
    private val repository: ShipmentInboundRepository
) : ViewModel() {
    private val _uiState = MutableStateFlow(ShipmentExceptionUiState())
    val uiState = _uiState.asStateFlow()

    private val _events = MutableSharedFlow<ShipmentExceptionEvent>(extraBufferCapacity = 1)
    val events = _events.asSharedFlow()

    private var uploadOperator: String = ""

    fun updateUploadOperator(account: String) {
        uploadOperator = account.trim()
    }

    fun updateSeqNo(seqNo: String) {
        _uiState.update {
            it.copy(seqNo = SequenceNumberUtil.normalize(seqNo), message = null)
        }
    }

    fun updateReason(reason: String) {
        _uiState.update {
            it.copy(reason = reason.trim(), message = null)
        }
    }

    fun updatePhoto(photo: Bitmap, photoBase64: String) {
        _uiState.update {
            it.copy(
                photoPreview = photo,
                photoBase64 = photoBase64,
                message = null
            )
        }
    }

    fun removePhoto() {
        _uiState.update {
            it.copy(photoPreview = null, photoBase64 = null, message = null)
        }
    }

    fun showMessage(message: String) {
        _uiState.update { it.copy(message = message) }
    }

    fun returnToMenu() {
        _events.tryEmit(ShipmentExceptionEvent.NavigateToMenu)
    }

    fun submit() {
        val currentState = _uiState.value
        val seqNo = currentState.seqNo.trim()
        val reason = currentState.reason.trim()
        val photoBase64 = currentState.photoBase64

        when {
            seqNo.isBlank() -> {
                _events.tryEmit(ShipmentExceptionEvent.ShowUploadResultDialog("請輸入流水號。", false))
                return
            }

            reason.isBlank() -> {
                _events.tryEmit(ShipmentExceptionEvent.ShowUploadResultDialog("請選擇異常原因。", false))
                return
            }

            photoBase64.isNullOrBlank() -> {
                _uiState.update { it.copy(message = "請先拍照。") }
                return
            }

            uploadOperator.isBlank() -> {
                _uiState.update { it.copy(message = "請先登入帳號。") }
                return
            }
        }

        viewModelScope.launch {
            _uiState.update { it.copy(isSubmitting = true, message = null) }

            when (
                val result = repository.submitInboundException(
                    ShipmentInboundExceptionRequest(
                        seqNo = seqNo,
                        reason = reason,
                        photo = photoBase64,
                        uploadOpe = uploadOperator
                    )
                )
            ) {
                is ApiResult.Success -> {
                    _uiState.update {
                        it.copy(isSubmitting = false, message = result.message)
                    }
                    _events.emit(ShipmentExceptionEvent.ShowUploadResultDialog(result.message, true))
                }

                is ApiResult.Failure -> {
                    _uiState.update {
                        it.copy(isSubmitting = false, message = result.message)
                    }
                    _events.emit(ShipmentExceptionEvent.ShowUploadResultDialog(result.message, false))
                }
            }
        }
    }

    fun continuePhoto() {
        _uiState.update {
            it.copy(photoPreview = null, photoBase64 = null, message = null)
        }
    }

    fun nextItem() {
        _uiState.value = ShipmentExceptionUiState()
    }

    companion object {
        fun factory(): ViewModelProvider.Factory {
            return object : ViewModelProvider.Factory {
                @Suppress("UNCHECKED_CAST")
                override fun <T : ViewModel> create(modelClass: Class<T>): T {
                    if (modelClass.isAssignableFrom(ShipmentExceptionViewModel::class.java)) {
                        return ShipmentExceptionViewModel(ServiceLocator.provideShipmentInboundRepository()) as T
                    }
                    throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
                }
            }
        }
    }
}
