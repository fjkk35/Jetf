package com.example.jetfapp.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.example.jetfapp.data.model.ApiResult
import com.example.jetfapp.data.model.UnpackingBagNoRequest
import com.example.jetfapp.data.model.UnpackingPendingRequest
import com.example.jetfapp.data.model.UnpackingTrackingNoRequest
import com.example.jetfapp.data.repository.UnpackingRepository
import com.example.jetfapp.di.ServiceLocator
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

enum class UnpackingMessageType {
    Info,
    Success,
    Error
}

data class UnpackingUiState(
    val dataTypes: List<String> = UnpackingViewModel.DATA_TYPES,
    val selectedDataType: String = "",
    val bagNo: String = "",
    val trackingNo: String = "",
    val isBagLocked: Boolean = false,
    val pendingTrackingNos: List<String> = emptyList(),
    val isSubmitting: Boolean = false,
    val isBackConfirmPending: Boolean = false,
    val message: String? = null,
    val messageType: UnpackingMessageType = UnpackingMessageType.Info
)

sealed interface UnpackingEvent {
    data object NavigateBack : UnpackingEvent
    data object FocusDataType : UnpackingEvent
    data object FocusBagNo : UnpackingEvent
    data object FocusTrackingNo : UnpackingEvent
}

class UnpackingViewModel(
    private val repository: UnpackingRepository
) : ViewModel() {
    private val _uiState = MutableStateFlow(UnpackingUiState())
    val uiState = _uiState.asStateFlow()

    private val _events = MutableSharedFlow<UnpackingEvent>(extraBufferCapacity = 4)
    val events = _events.asSharedFlow()

    private var uploadOpe: String = ""

    fun updateUploadOpe(account: String) {
        uploadOpe = account.trim()
    }

    fun selectDataType(dataType: String) {
        if (_uiState.value.isBagLocked) {
            return
        }

        _uiState.update {
            it.copy(
                selectedDataType = dataType.trim().uppercase(),
                message = null,
                messageType = UnpackingMessageType.Info
            )
        }
        _events.tryEmit(UnpackingEvent.FocusBagNo)
    }

    fun updateBagNo(bagNo: String) {
        if (_uiState.value.isBagLocked) {
            return
        }
        _uiState.update { it.copy(bagNo = bagNo.trim().uppercase()) }
    }

    fun updateTrackingNo(trackingNo: String) {
        _uiState.update { it.copy(trackingNo = trackingNo.trim().uppercase()) }
    }

    fun submitBagNo(bagNoInput: String? = null) {
        val state = _uiState.value
        if (state.isSubmitting || state.isBagLocked) {
            return
        }
        if (!validateCommon()) {
            return
        }

        val bagNo = (bagNoInput ?: state.bagNo).trim().uppercase()
        if (bagNo.isBlank()) {
            showMessage("請掃描袋號。", UnpackingMessageType.Error)
            _events.tryEmit(UnpackingEvent.FocusBagNo)
            return
        }

        _uiState.update {
            it.copy(bagNo = bagNo, isSubmitting = true, message = null, isBackConfirmPending = false)
        }

        viewModelScope.launch {
            when (
                val result = repository.scanBagNo(
                    UnpackingBagNoRequest(
                        dataType = state.selectedDataType,
                        bagNo = bagNo,
                        uploadOpe = uploadOpe
                    )
                )
            ) {
                is ApiResult.Success -> {
                    val pending = result.data.pendingTrackingNos.orEmpty()
                    if (result.data.needUnpacking) {
                        _uiState.update {
                            it.copy(
                                isSubmitting = false,
                                isBagLocked = true,
                                trackingNo = "",
                                pendingTrackingNos = pending,
                                message = "拆袋處理",
                                messageType = UnpackingMessageType.Success
                            )
                        }
                        _events.emit(UnpackingEvent.FocusTrackingNo)
                    } else {
                        _uiState.update {
                            it.copy(
                                isSubmitting = false,
                                pendingTrackingNos = emptyList(),
                                message = "不需拆袋",
                                messageType = UnpackingMessageType.Info
                            )
                        }
                        _events.emit(UnpackingEvent.FocusBagNo)
                    }
                }

                is ApiResult.Failure -> {
                    _uiState.update {
                        it.copy(
                            isSubmitting = false,
                            message = result.message,
                            messageType = UnpackingMessageType.Error
                        )
                    }
                    _events.emit(UnpackingEvent.FocusBagNo)
                }
            }
        }
    }

    fun submitTrackingNo(trackingNoInput: String? = null) {
        val state = _uiState.value
        if (state.isSubmitting) {
            return
        }
        if (!state.isBagLocked) {
            submitBagNo()
            return
        }
        if (!validateCommon()) {
            return
        }

        val trackingNo = (trackingNoInput ?: state.trackingNo).trim().uppercase()
        if (trackingNo.isBlank()) {
            showMessage("請掃描分提單號。", UnpackingMessageType.Error)
            _events.tryEmit(UnpackingEvent.FocusTrackingNo)
            return
        }

        _uiState.update {
            it.copy(trackingNo = trackingNo, isSubmitting = true, message = null, isBackConfirmPending = false)
        }

        viewModelScope.launch {
            when (
                val result = repository.scanTrackingNo(
                    UnpackingTrackingNoRequest(
                        dataType = state.selectedDataType,
                        bagNo = state.bagNo,
                        trackingNo = trackingNo,
                        uploadOpe = uploadOpe
                    )
                )
            ) {
                is ApiResult.Success -> {
                    val message = if (result.data.isUnpackTarget) {
                        // 有備註顯示備註，否則顯示「拆出」
                        result.data.resultMessage?.trim().orEmpty().ifEmpty { "拆出" }
                    } else {
                        "非拆出貨件"
                    }
                    _uiState.update {
                        it.copy(
                            isSubmitting = false,
                            trackingNo = "",
                            // 拆出的分提單號直接從待掃描清單移除，不再向 DB 查詢
                            pendingTrackingNos = if (result.data.isUnpackTarget) {
                                it.pendingTrackingNos.filterNot { pendingNo ->
                                    pendingNo.equals(trackingNo, ignoreCase = true)
                                }
                            } else {
                                it.pendingTrackingNos
                            },
                            message = message,
                            messageType = if (result.data.isUnpackTarget) {
                                UnpackingMessageType.Success
                            } else {
                                UnpackingMessageType.Info
                            }
                        )
                    }
                    _events.emit(UnpackingEvent.FocusTrackingNo)
                }

                is ApiResult.Failure -> {
                    _uiState.update {
                        it.copy(
                            isSubmitting = false,
                            message = result.message,
                            messageType = UnpackingMessageType.Error
                        )
                    }
                    _events.emit(UnpackingEvent.FocusTrackingNo)
                }
            }
        }
    }

    /**
     * 返回：
     * - 尚未鎖定袋號時離開畫面。
     * - 已鎖定袋號時重新向 DB 檢查是否還有未掃描的分提單號；
     *   全部完成則回到袋號掃描，尚有未掃描則列出並需再按一次返回才結束此袋號。
     */
    fun onBackRequested() {
        val state = _uiState.value
        if (state.isSubmitting) {
            return
        }

        if (!state.isBagLocked) {
            _events.tryEmit(UnpackingEvent.NavigateBack)
            return
        }

        if (state.isBackConfirmPending) {
            // 回到袋號，清空內容與訊息
            resetBag(
                message = null,
                messageType = UnpackingMessageType.Info,
                keepPending = false
            )
            return
        }

        _uiState.update { it.copy(isSubmitting = true, message = null) }

        viewModelScope.launch {
            when (
                val result = repository.getPending(
                    UnpackingPendingRequest(
                        dataType = state.selectedDataType,
                        bagNo = state.bagNo,
                        uploadOpe = uploadOpe
                    )
                )
            ) {
                is ApiResult.Success -> {
                    val pending = result.data.orEmpty()
                    if (pending.isEmpty()) {
                        resetBag(
                            message = "已全部掃描完成",
                            messageType = UnpackingMessageType.Success,
                            keepPending = false
                        )
                    } else {
                        _uiState.update {
                            it.copy(
                                isSubmitting = false,
                                pendingTrackingNos = pending,
                                isBackConfirmPending = true,
                                message = "尚有 ${pending.size} 筆未掃描，再按返回結束",
                                messageType = UnpackingMessageType.Error
                            )
                        }
                        _events.emit(UnpackingEvent.FocusTrackingNo)
                    }
                }

                is ApiResult.Failure -> {
                    _uiState.update {
                        it.copy(
                            isSubmitting = false,
                            isBackConfirmPending = true,
                            message = "${result.message}，再按返回結束",
                            messageType = UnpackingMessageType.Error
                        )
                    }
                    _events.emit(UnpackingEvent.FocusTrackingNo)
                }
            }
        }
    }

    private fun resetBag(message: String?, messageType: UnpackingMessageType, keepPending: Boolean) {
        _uiState.update {
            it.copy(
                bagNo = "",
                trackingNo = "",
                isBagLocked = false,
                isSubmitting = false,
                isBackConfirmPending = false,
                pendingTrackingNos = if (keepPending) it.pendingTrackingNos else emptyList(),
                message = message,
                messageType = messageType
            )
        }
        _events.tryEmit(UnpackingEvent.FocusBagNo)
    }

    private fun validateCommon(): Boolean {
        if (_uiState.value.selectedDataType.isBlank()) {
            showMessage("請先選擇作業地區。", UnpackingMessageType.Error)
            _events.tryEmit(UnpackingEvent.FocusDataType)
            return false
        }
        if (uploadOpe.isBlank()) {
            showMessage("請先登入帳號。", UnpackingMessageType.Error)
            return false
        }
        return true
    }

    private fun showMessage(message: String, messageType: UnpackingMessageType) {
        _uiState.update { it.copy(message = message, messageType = messageType) }
    }

    companion object {
        val DATA_TYPES = listOf("FTZ", "TACT")

        fun factory(): ViewModelProvider.Factory {
            return object : ViewModelProvider.Factory {
                @Suppress("UNCHECKED_CAST")
                override fun <T : ViewModel> create(modelClass: Class<T>): T {
                    if (modelClass.isAssignableFrom(UnpackingViewModel::class.java)) {
                        return UnpackingViewModel(ServiceLocator.provideUnpackingRepository()) as T
                    }
                    throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
                }
            }
        }
    }
}
