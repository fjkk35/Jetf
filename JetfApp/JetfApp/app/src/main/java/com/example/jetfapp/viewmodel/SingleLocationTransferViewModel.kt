package com.example.jetfapp.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.example.jetfapp.data.model.ApiResult
import com.example.jetfapp.data.model.UpdateLocationCodeRequest
import com.example.jetfapp.data.repository.ShipmentInboundRepository
import com.example.jetfapp.di.ServiceLocator
import com.example.jetfapp.utils.SequenceNumberUtil
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class SingleLocationTransferUiState(
    val locationCode: String = "",
    val seqNo: String = "",
    val isLocationLocked: Boolean = false,
    val isSubmitting: Boolean = false,
    val message: String? = null
)

sealed interface SingleLocationTransferEvent {
    data object NavigateBack : SingleLocationTransferEvent
    data object FocusLocation : SingleLocationTransferEvent
    data object FocusSeqNo : SingleLocationTransferEvent
}

class SingleLocationTransferViewModel(
    private val repository: ShipmentInboundRepository
) : ViewModel() {
    private val _uiState = MutableStateFlow(SingleLocationTransferUiState())
    val uiState = _uiState.asStateFlow()

    private val _events = MutableSharedFlow<SingleLocationTransferEvent>(extraBufferCapacity = 1)
    val events = _events.asSharedFlow()

    private var editUser: String = ""

    fun updateEditUser(account: String) {
        editUser = account.trim()
    }

    fun updateLocationCode(locationCode: String) {
        _uiState.update {
            it.copy(locationCode = normalizeLocationCode(locationCode), message = null)
        }
    }

    fun updateSeqNo(seqNo: String) {
        _uiState.update {
            it.copy(seqNo = SequenceNumberUtil.normalize(seqNo), message = null)
        }
    }

    fun lockLocation(locationCodeInput: String? = null): Boolean {
        val locationCode = normalizeLocationCode(locationCodeInput ?: _uiState.value.locationCode)
        if (locationCode.isBlank()) {
            _uiState.update { it.copy(message = "請輸入新儲位。") }
            _events.tryEmit(SingleLocationTransferEvent.FocusLocation)
            return false
        }

        _uiState.update {
            it.copy(
                locationCode = locationCode,
                isLocationLocked = true,
                message = "新儲位已鎖定，請掃描流水號。"
            )
        }
        _events.tryEmit(SingleLocationTransferEvent.FocusSeqNo)
        return true
    }

    fun unlockLocation() {
        _uiState.update {
            it.copy(
                isLocationLocked = false,
                seqNo = "",
                message = "已解除新儲位鎖定，請重新輸入。"
            )
        }
        _events.tryEmit(SingleLocationTransferEvent.FocusLocation)
    }

    fun submit(seqNoInput: String? = null) {
        if (!_uiState.value.isLocationLocked && !lockLocation()) {
            return
        }

        val currentState = _uiState.value
        val seqNo = SequenceNumberUtil.normalize(seqNoInput ?: currentState.seqNo)
        if (seqNo.isBlank()) {
            _uiState.update { it.copy(message = "請輸入流水號。") }
            _events.tryEmit(SingleLocationTransferEvent.FocusSeqNo)
            return
        }

        if (editUser.isBlank()) {
            _uiState.update { it.copy(message = "請先登入帳號。") }
            return
        }

        _uiState.update {
            it.copy(seqNo = seqNo, isSubmitting = true, message = null)
        }

        viewModelScope.launch {
            when (
                val result = repository.updateLocationCode(
                    UpdateLocationCodeRequest(
                        seqNo = seqNo,
                        locationCode = currentState.locationCode,
                        editUser = editUser
                    )
                )
            ) {
                is ApiResult.Success -> {
                    _uiState.update {
                        it.copy(seqNo = "", isSubmitting = false, message = result.message)
                    }
                    _events.emit(SingleLocationTransferEvent.FocusSeqNo)
                }

                is ApiResult.Failure -> {
                    _uiState.update {
                        it.copy(seqNo = seqNo, isSubmitting = false, message = result.message)
                    }
                    _events.emit(SingleLocationTransferEvent.FocusSeqNo)
                }
            }
        }
    }

    fun returnToMenu() {
        _events.tryEmit(SingleLocationTransferEvent.NavigateBack)
    }

    private fun normalizeLocationCode(locationCode: String): String {
        return locationCode.trim().uppercase()
    }

    companion object {
        fun factory(): ViewModelProvider.Factory {
            return object : ViewModelProvider.Factory {
                @Suppress("UNCHECKED_CAST")
                override fun <T : ViewModel> create(modelClass: Class<T>): T {
                    if (modelClass.isAssignableFrom(SingleLocationTransferViewModel::class.java)) {
                        return SingleLocationTransferViewModel(ServiceLocator.provideShipmentInboundRepository()) as T
                    }
                    throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
                }
            }
        }
    }
}
