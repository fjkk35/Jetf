package com.example.jetfapp.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.example.jetfapp.data.model.ApiResult
import com.example.jetfapp.data.model.ShipmentInboundRequest
import com.example.jetfapp.data.model.SourceType
import com.example.jetfapp.data.repository.ShipmentInboundRepository
import com.example.jetfapp.di.ServiceLocator
import com.example.jetfapp.utils.SequenceNumberUtil
import java.time.OffsetDateTime
import java.time.format.DateTimeFormatter
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class InboundSettingsUiState(
    val isLoading: Boolean = false,
    val sourceTypes: List<SourceType> = emptyList(),
    val selectedSourceName: String = "",
    val startSequence: String = "",
    val message: String? = null
)

data class InboundWorkUiState(
    val sourceTypeId: Int? = null,
    val sourceTypeName: String = "",
    val currentSequence: String = "",
    val locationCode: String = "",
    val isLocationLocked: Boolean = false,
    val trackingNo: String = "",
    val returnTrackingNo: String = "",
    val showReturnTracking: Boolean = false,
    val isSubmitting: Boolean = false,
    val message: String? = null,
    val isSequenceLimitReached: Boolean = false
)

sealed interface ShipmentInboundEvent {
    data object NavigateToMenu : ShipmentInboundEvent
    data object NavigateToWork : ShipmentInboundEvent
    data object NavigateToSettings : ShipmentInboundEvent
    data class ShowUnknownShipmentDialog(val trackingNo: String) : ShipmentInboundEvent
}

class ShipmentInboundViewModel(
    private val repository: ShipmentInboundRepository
) : ViewModel() {
    private val _settingsState = MutableStateFlow(InboundSettingsUiState())
    val settingsState = _settingsState.asStateFlow()

    private val _workState = MutableStateFlow(InboundWorkUiState())
    val workState = _workState.asStateFlow()

    private val _events = MutableSharedFlow<ShipmentInboundEvent>(extraBufferCapacity = 1)
    val events = _events.asSharedFlow()

    private var pendingUnknownTrackingNo: String? = null

    fun loadSourceTypes(forceReload: Boolean = false) {
        if (_settingsState.value.sourceTypes.isNotEmpty() && !forceReload) {
            return
        }

        viewModelScope.launch {
            _settingsState.update { it.copy(isLoading = true, message = null) }
            when (val result = repository.getSourceTypes()) {
                is ApiResult.Success -> {
                    val sourceTypes = result.data
                    _settingsState.update { currentState ->
                        val selectedSourceName = currentState.selectedSourceName
                            .takeIf { name -> sourceTypes.any { it.sourceType == name } }
                            .orEmpty()
                        currentState.copy(
                            isLoading = false,
                            sourceTypes = sourceTypes,
                            selectedSourceName = selectedSourceName,
                            message = null
                        )
                    }
                }

                is ApiResult.Failure -> {
                    _settingsState.update {
                        it.copy(isLoading = false, message = result.message)
                    }
                }
            }
        }
    }

    fun updateSelectedSourceName(sourceName: String) {
        _settingsState.update { it.copy(selectedSourceName = sourceName, message = null) }
    }

    fun updateStartSequence(startSequence: String) {
        _settingsState.update {
            it.copy(
                startSequence = SequenceNumberUtil.normalize(startSequence),
                message = null
            )
        }
    }

    fun confirmSettings() {
        val state = _settingsState.value
        val sourceType = state.sourceTypes.firstOrNull { it.sourceType == state.selectedSourceName }
        if (sourceType == null) {
            _settingsState.update { it.copy(message = "請選擇貨件來源。") }
            return
        }

        if (!SequenceNumberUtil.isValid(state.startSequence)) {
            _settingsState.update { it.copy(message = "請輸入正確的流水號格式，例如 AB0001。") }
            return
        }

        val normalizedSequence = SequenceNumberUtil.normalize(state.startSequence)
        _workState.value = InboundWorkUiState(
            sourceTypeId = sourceType.id,
            sourceTypeName = sourceType.sourceType,
            currentSequence = normalizedSequence,
            showReturnTracking = sourceType.sourceType == "新竹退件"
        )
        _events.tryEmit(ShipmentInboundEvent.NavigateToWork)
    }

    fun returnToMenu() {
        _events.tryEmit(ShipmentInboundEvent.NavigateToMenu)
    }

    fun returnToSettings() {
        _events.tryEmit(ShipmentInboundEvent.NavigateToSettings)
    }

    fun updateLocation(locationCode: String) {
        _workState.update { currentState ->
            currentState.copy(locationCode = locationCode.trim().uppercase(), message = null)
        }
    }

    fun lockLocation(locationCodeInput: String? = null): Boolean {
        val locationCode = (locationCodeInput ?: _workState.value.locationCode).trim().uppercase()
        if (locationCode.isBlank()) {
            _workState.update { it.copy(message = "請先輸入儲位。") }
            return false
        }

        _workState.update {
            it.copy(
                locationCode = locationCode,
                isLocationLocked = true,
                message = "儲位已自動鎖定，可開始掃描單號。"
            )
        }
        return true
    }

    fun unlockLocation() {
        _workState.update {
            it.copy(isLocationLocked = false, message = "已解除儲位設定，請重新輸入。")
        }
    }

    fun updateTracking(trackingNo: String) {
        _workState.update {
            it.copy(trackingNo = trackingNo.trim(), message = null)
        }
    }

    fun updateReturnTracking(returnTrackingNo: String) {
        _workState.update {
            it.copy(returnTrackingNo = returnTrackingNo.trim(), message = null)
        }
    }

    fun applyTrackingInput(trackingNo: String): Boolean {
        if (_workState.value.isSequenceLimitReached) {
            _workState.update { it.copy(message = "流水號為最後號，請到上一步變更") }
            return false
        }

        if (!_workState.value.isLocationLocked && !lockLocation()) {
            return false
        }

        val normalizedTrackingNo = trackingNo.trim()
        if (normalizedTrackingNo.isBlank()) {
            _workState.update { it.copy(message = "請輸入或掃描單號。") }
            return false
        }

        val latestState = _workState.value
        val requiresReturnTracking = latestState.showReturnTracking

        _workState.update {
            it.copy(
                trackingNo = normalizedTrackingNo,
                returnTrackingNo = if (it.showReturnTracking) "" else it.returnTrackingNo,
                message = if (requiresReturnTracking) "請輸入或掃描退件單號。" else null
            )
        }

        if (requiresReturnTracking) {
            return true
        }

        submitTracking(normalizedTrackingNo)
        return false
    }

    fun applyReturnTrackingAndSubmit(returnTrackingNo: String) {
        val normalizedReturnTrackingNo = returnTrackingNo.trim()
        if (normalizedReturnTrackingNo.isBlank()) {
            _workState.update { it.copy(message = "此貨件來源需輸入退件單號。") }
            return
        }

        _workState.update {
            it.copy(returnTrackingNo = normalizedReturnTrackingNo, message = null)
        }
        submitTracking()
    }

    fun submitTracking(inputTrackingNo: String? = null) {
        if (_workState.value.isSequenceLimitReached) {
            _workState.update { it.copy(message = "流水號為最後號，請到上一步變更") }
            return
        }

        if (!_workState.value.isLocationLocked && !lockLocation()) {
            return
        }

        val currentState = _workState.value
        val trackingNo = (inputTrackingNo ?: currentState.trackingNo).trim()
        if (trackingNo.isBlank()) {
            _workState.update { it.copy(message = "請輸入或掃描單號。") }
            return
        }

        val returnTrackingNo = currentState.returnTrackingNo.trim()
        if (currentState.showReturnTracking && returnTrackingNo.isBlank()) {
            _workState.update { it.copy(message = "此貨件來源需輸入退件單號。") }
            return
        }

        _workState.update {
            it.copy(
                trackingNo = trackingNo,
                returnTrackingNo = returnTrackingNo,
                isSubmitting = true,
                message = null
            )
        }

        viewModelScope.launch {
            when (val result = repository.checkShipment(trackingNo)) {
                is ApiResult.Success -> {
                    if (result.data) {
                        submitInbound(trackingNo)
                    } else {
                        pendingUnknownTrackingNo = trackingNo
                        _workState.update { it.copy(isSubmitting = false) }
                        _events.emit(ShipmentInboundEvent.ShowUnknownShipmentDialog(trackingNo))
                    }
                }

                is ApiResult.Failure -> {
                    _workState.update {
                        it.copy(isSubmitting = false, message = result.message)
                    }
                }
            }
        }
    }

    fun confirmUnknownShipment() {
        val trackingNo = pendingUnknownTrackingNo ?: _workState.value.trackingNo
        pendingUnknownTrackingNo = null
        submitInbound(trackingNo)
    }

    fun cancelUnknownShipment() {
        pendingUnknownTrackingNo = null
        _workState.update { it.copy(isSubmitting = false) }
    }

    private fun submitInbound(trackingNo: String) {
        val currentState = _workState.value
        val request = ShipmentInboundRequest(
            inboundDate = OffsetDateTime.now().format(DateTimeFormatter.ISO_OFFSET_DATE_TIME),
            trackingNo = trackingNo,
            seqNo = currentState.currentSequence,
            locationCode = currentState.locationCode,
            sourceType = currentState.sourceTypeId,
            returnTrackingNo = currentState.returnTrackingNo.trim()
        )

        viewModelScope.launch {
            when (val result = repository.submitInbound(request)) {
                is ApiResult.Success -> handleWriteSuccess(result.message)
                is ApiResult.Failure -> {
                    _workState.update {
                        it.copy(isSubmitting = false, message = result.message)
                    }
                }
            }
        }
    }

    private fun handleWriteSuccess(message: String) {
        val currentState = _workState.value
        val nextSequence = SequenceNumberUtil.nextOrNull(currentState.currentSequence)
        if (nextSequence == null) {
            _workState.update {
                it.copy(
                    isSubmitting = false,
                    trackingNo = "",
                    returnTrackingNo = "",
                    message = "流水號為最後號，請到上一步變更",
                    isSequenceLimitReached = true
                )
            }
            return
        }

        _workState.update {
            it.copy(
                currentSequence = nextSequence,
                trackingNo = "",
                returnTrackingNo = "",
                isSubmitting = false,
                message = if (message.isBlank()) "入庫成功" else message
            )
        }
    }

    companion object {
        fun factory(): ViewModelProvider.Factory {
            return object : ViewModelProvider.Factory {
                @Suppress("UNCHECKED_CAST")
                override fun <T : ViewModel> create(modelClass: Class<T>): T {
                    if (modelClass.isAssignableFrom(ShipmentInboundViewModel::class.java)) {
                        return ShipmentInboundViewModel(ServiceLocator.provideShipmentInboundRepository()) as T
                    }
                    throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
                }
            }
        }
    }
}