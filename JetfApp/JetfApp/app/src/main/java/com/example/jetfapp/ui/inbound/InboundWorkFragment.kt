package com.example.jetfapp.ui.inbound

import android.os.Bundle
import android.view.KeyEvent
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.view.inputmethod.EditorInfo
import androidx.core.view.isVisible
import androidx.core.widget.doAfterTextChanged
import androidx.fragment.app.Fragment
import androidx.fragment.app.activityViewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import com.example.jetfapp.MainActivity
import com.example.jetfapp.R
import com.example.jetfapp.databinding.FragmentInboundWorkBinding
import com.example.jetfapp.ui.common.FunctionKey
import com.example.jetfapp.ui.common.FunctionKeyHandler
import com.example.jetfapp.ui.common.KeyboardWedgeScanHandler
import com.example.jetfapp.ui.common.ScanInputHandler
import com.example.jetfapp.ui.common.hideKeyboard
import com.example.jetfapp.ui.common.showKeyboard
import com.example.jetfapp.viewmodel.AppViewModel
import com.example.jetfapp.viewmodel.ShipmentInboundEvent
import com.example.jetfapp.viewmodel.ShipmentInboundViewModel
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import kotlinx.coroutines.launch

class InboundWorkFragment : Fragment(), FunctionKeyHandler, KeyboardWedgeScanHandler {
    private var _binding: FragmentInboundWorkBinding? = null
    private var isApplyingUiState = false
    private val binding: FragmentInboundWorkBinding
        get() = checkNotNull(_binding)

    private val shipmentInboundViewModel: ShipmentInboundViewModel by activityViewModels {
        ShipmentInboundViewModel.factory()
    }
    private val appViewModel: AppViewModel by activityViewModels {
        AppViewModel.factory()
    }

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentInboundWorkBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        shipmentInboundViewModel.updateUploadOperator(appViewModel.currentAccount.value.orEmpty())
        binding.root.isFocusable = true
        binding.root.isFocusableInTouchMode = true
        binding.editTracking.showSoftInputOnFocus = false
        binding.editReturnTracking.showSoftInputOnFocus = false
        binding.editLocation.doAfterTextChanged { editable ->
            if (isApplyingUiState) {
                return@doAfterTextChanged
            }
            shipmentInboundViewModel.updateLocation(editable?.toString().orEmpty())
        }
        binding.editLocation.setOnFocusChangeListener { _, hasFocus ->
            syncBottomActionBarSuppressed()
            if (hasFocus) {
                binding.editLocation.selectAll()
            } else if (binding.editLocation.text?.isNotBlank() == true) {
                if (shipmentInboundViewModel.lockLocation()) {
                    focusTrackingField()
                }
            }
        }
        binding.editLocation.setOnClickListener {
            binding.editLocation.selectAll()
        }
        binding.editLocation.setOnEditorActionListener { _, actionId, event ->
            val isImeNext = actionId == EditorInfo.IME_ACTION_NEXT
            val isImeDone = actionId == EditorInfo.IME_ACTION_DONE
            val isEnterKey = event?.keyCode == KeyEvent.KEYCODE_ENTER
            if (isImeNext || isImeDone || isEnterKey) {
                if (shipmentInboundViewModel.lockLocation()) {
                    focusTrackingField()
                }
                true
            } else {
                false
            }
        }
        binding.editTracking.doAfterTextChanged { editable ->
            if (isApplyingUiState) {
                return@doAfterTextChanged
            }
            shipmentInboundViewModel.updateTracking(editable?.toString().orEmpty())
        }
        binding.editTracking.setOnFocusChangeListener { _, hasFocus ->
            syncBottomActionBarSuppressed()
            if (hasFocus) {
                binding.editTracking.selectAll()
            }
        }
        binding.editTracking.setOnClickListener {
            binding.editTracking.selectAll()
        }
        binding.editReturnTracking.doAfterTextChanged { editable ->
            if (isApplyingUiState) {
                return@doAfterTextChanged
            }
            shipmentInboundViewModel.updateReturnTracking(editable?.toString().orEmpty())
        }
        binding.editReturnTracking.setOnFocusChangeListener { _, hasFocus ->
            syncBottomActionBarSuppressed()
            if (hasFocus) {
                binding.editReturnTracking.selectAll()
            }
        }
        binding.editReturnTracking.setOnClickListener {
            binding.editReturnTracking.selectAll()
        }
        binding.editTracking.setOnEditorActionListener { _, actionId, event ->
            val isImeDone = actionId == EditorInfo.IME_ACTION_DONE
            val isEnterKey = event?.keyCode == KeyEvent.KEYCODE_ENTER
            if (isImeDone || isEnterKey) {
                handleTrackingCaptured(binding.editTracking.text?.toString().orEmpty())
                true
            } else {
                false
            }
        }
        binding.editReturnTracking.setOnEditorActionListener { _, actionId, event ->
            val isImeDone = actionId == EditorInfo.IME_ACTION_DONE
            val isEnterKey = event?.keyCode == KeyEvent.KEYCODE_ENTER
            if (isImeDone || isEnterKey) {
                shipmentInboundViewModel.applyReturnTrackingAndSubmit(binding.editReturnTracking.text?.toString().orEmpty())
                true
            } else {
                false
            }
        }

        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch {
                    shipmentInboundViewModel.workState.collect { state ->
                        isApplyingUiState = true
                        try {
                            binding.textSource.text = getString(R.string.label_source_type) + "：" + state.sourceTypeName
                            binding.textSequence.text = getString(R.string.label_sequence) + "：" + state.currentSequence
                            if (binding.editLocation.text?.toString() != state.locationCode) {
                                binding.editLocation.setText(state.locationCode)
                                binding.editLocation.setSelection(state.locationCode.length)
                            }
                            if (binding.editTracking.text?.toString() != state.trackingNo) {
                                binding.editTracking.setText(state.trackingNo)
                                binding.editTracking.setSelection(state.trackingNo.length)
                            }
                            if (binding.editReturnTracking.text?.toString() != state.returnTrackingNo) {
                                binding.editReturnTracking.setText(state.returnTrackingNo)
                                binding.editReturnTracking.setSelection(state.returnTrackingNo.length)
                            }

                            binding.editLocation.isEnabled = !state.isLocationLocked
                            binding.editTracking.isEnabled = state.isLocationLocked && !state.isSequenceLimitReached && !state.isSubmitting
                            binding.inputLayoutReturnTracking.isVisible = state.showReturnTracking
                            binding.editReturnTracking.isEnabled = state.isLocationLocked && state.showReturnTracking && !state.isSubmitting
                            binding.textMessage.text = state.message.orEmpty()
                            binding.textMessage.setTextColor(
                                requireContext().getColor(
                                    if (state.message.isErrorMessage()) R.color.jetf_error else R.color.jetf_ink
                                )
                            )
                            binding.loadingContainer.isVisible = state.isSubmitting

                            if (state.message.isErrorMessage()) {
                                (activity as? MainActivity)?.setBottomActionBarSuppressed(false)
                                binding.editLocation.clearFocus()
                                binding.editTracking.clearFocus()
                                binding.editReturnTracking.clearFocus()
                                hideKeyboard(binding.root)
                            }
                        } finally {
                            isApplyingUiState = false
                        }
                    }
                }

                launch {
                    shipmentInboundViewModel.events.collect { event ->
                        when (event) {
                            ShipmentInboundEvent.NavigateToSettings -> requireActivity().onBackPressedDispatcher.onBackPressed()
                            is ShipmentInboundEvent.ShowSubmissionSuccess -> Unit
                            is ShipmentInboundEvent.ShowUnknownShipmentDialog -> showUnknownShipmentDialog(event.trackingNo)
                            ShipmentInboundEvent.NavigateToMenu -> Unit
                            ShipmentInboundEvent.NavigateToWork -> Unit
                        }
                    }
                }
            }
        }
    }

    override fun onFunctionKeyPressed(functionKey: FunctionKey) {
        when (functionKey) {
            FunctionKey.F3 -> shipmentInboundViewModel.returnToSettings()
            FunctionKey.F4 -> {
                shipmentInboundViewModel.unlockLocation()
                binding.editLocation.requestFocus()
                binding.editLocation.selectAll()
                syncBottomActionBarSuppressed()
                showKeyboard(binding.editLocation)
            }
        }
    }

    override fun onScanReceived(scanValue: String) {
        if (shipmentInboundViewModel.workState.value.isSubmitting) {
            return
        }

        val normalized = scanValue.trim()
        val state = shipmentInboundViewModel.workState.value

        when {
            !state.isLocationLocked -> {
                binding.editLocation.setText(normalized)
                binding.editLocation.setSelection(normalized.length)
                if (shipmentInboundViewModel.lockLocation(normalized)) {
                    focusTrackingField()
                }
            }

            binding.editReturnTracking.hasFocus() ||
                (state.showReturnTracking && state.trackingNo.isNotBlank() && state.returnTrackingNo.isBlank()) -> {
                binding.editReturnTracking.setText(normalized)
                binding.editReturnTracking.setSelection(normalized.length)
                shipmentInboundViewModel.applyReturnTrackingAndSubmit(normalized)
            }

            else -> {
                binding.editTracking.setText(normalized)
                binding.editTracking.setSelection(normalized.length)
                handleTrackingCaptured(normalized)
            }
        }
    }

    override fun shouldConsumeWedgeInput(): Boolean {
        val state = shipmentInboundViewModel.workState.value
        return state.isLocationLocked && !state.isSubmitting && !state.isSequenceLimitReached
    }

    private fun handleTrackingCaptured(trackingNo: String) {
        val requiresReturnTracking = shipmentInboundViewModel.applyTrackingInput(trackingNo)
        if (requiresReturnTracking) {
            focusReturnTrackingField()
        }
    }

    private fun focusTrackingField() {
        binding.editTracking.clearFocus()
        binding.editReturnTracking.clearFocus()
        binding.root.requestFocus()
        syncBottomActionBarSuppressed()
        hideKeyboard(binding.root)
    }

    private fun focusReturnTrackingField() {
        binding.editTracking.clearFocus()
        binding.editReturnTracking.clearFocus()
        binding.root.requestFocus()
        syncBottomActionBarSuppressed()
        hideKeyboard(binding.root)
    }

    private fun showUnknownShipmentDialog(trackingNo: String) {
        val dialog = MaterialAlertDialogBuilder(requireContext())
            .setTitle(getString(R.string.message_unknown_shipment_title, trackingNo))
            .setMessage(getString(R.string.message_unknown_shipment))
            .setNegativeButton(R.string.action_cancel) { _, _ ->
                shipmentInboundViewModel.cancelUnknownShipment()
            }
            .setPositiveButton(R.string.action_confirm) { _, _ ->
                shipmentInboundViewModel.confirmUnknownShipment()
            }
            .show()

        dialog.getButton(androidx.appcompat.app.AlertDialog.BUTTON_NEGATIVE)?.apply {
            isFocusable = false
            isFocusableInTouchMode = false
            clearFocus()
        }
        dialog.getButton(androidx.appcompat.app.AlertDialog.BUTTON_POSITIVE)?.apply {
            isFocusable = false
            isFocusableInTouchMode = false
            clearFocus()
        }
        dialog.window?.decorView?.clearFocus()
    }

    override fun onDestroyView() {
        (activity as? MainActivity)?.setBottomActionBarSuppressed(false)
        _binding = null
        super.onDestroyView()
    }

    private fun syncBottomActionBarSuppressed() {
        val hasFocusedInput = binding.editLocation.hasFocus() || binding.editReturnTracking.hasFocus()
        (activity as? MainActivity)?.setBottomActionBarSuppressed(hasFocusedInput)
    }

    private fun String?.isErrorMessage(): Boolean {
        val message = this?.trim().orEmpty()
        if (message.isBlank()) {
            return false
        }

        if (message.startsWith("新增成功")) {
            return false
        }

        return message !in setOf(
            "儲位已自動鎖定，可開始掃描單號。",
            "已解除儲位設定，請重新輸入。",
            "入庫成功",
            "入庫資料寫入成功"
        )
    }
}