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
import com.example.jetfapp.R
import com.example.jetfapp.databinding.FragmentInboundWorkBinding
import com.example.jetfapp.ui.common.FunctionKey
import com.example.jetfapp.ui.common.FunctionKeyHandler
import com.example.jetfapp.ui.common.ScanInputHandler
import com.example.jetfapp.viewmodel.ShipmentInboundEvent
import com.example.jetfapp.viewmodel.ShipmentInboundViewModel
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import kotlinx.coroutines.launch

class InboundWorkFragment : Fragment(), FunctionKeyHandler, ScanInputHandler {
    private var _binding: FragmentInboundWorkBinding? = null
    private val binding: FragmentInboundWorkBinding
        get() = checkNotNull(_binding)

    private val shipmentInboundViewModel: ShipmentInboundViewModel by activityViewModels {
        ShipmentInboundViewModel.factory()
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
        binding.buttonLockLocation.setOnClickListener {
            shipmentInboundViewModel.lockLocation()
        }
        binding.editLocation.doAfterTextChanged { editable ->
            shipmentInboundViewModel.updateLocation(editable?.toString().orEmpty())
        }
        binding.editTracking.doAfterTextChanged { editable ->
            shipmentInboundViewModel.updateTracking(editable?.toString().orEmpty())
        }
        binding.editReturnTracking.doAfterTextChanged { editable ->
            shipmentInboundViewModel.updateReturnTracking(editable?.toString().orEmpty())
        }
        binding.editTracking.setOnEditorActionListener { _, actionId, event ->
            val isImeDone = actionId == EditorInfo.IME_ACTION_DONE
            val isEnterKey = event?.keyCode == KeyEvent.KEYCODE_ENTER
            if (isImeDone || isEnterKey) {
                shipmentInboundViewModel.submitTracking()
                true
            } else {
                false
            }
        }

        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch {
                    shipmentInboundViewModel.workState.collect { state ->
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
                        binding.buttonLockLocation.isEnabled = !state.isLocationLocked
                        binding.editTracking.isEnabled = state.isLocationLocked && !state.isSequenceLimitReached && !state.isSubmitting
                        binding.inputLayoutReturnTracking.isVisible = state.showReturnTracking
                        binding.editReturnTracking.isEnabled = state.isLocationLocked && state.showReturnTracking && !state.isSubmitting
                        binding.textMessage.text = state.message.orEmpty()
                    }
                }

                launch {
                    shipmentInboundViewModel.events.collect { event ->
                        when (event) {
                            ShipmentInboundEvent.NavigateToSettings -> requireActivity().onBackPressedDispatcher.onBackPressed()
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
            FunctionKey.F4 -> shipmentInboundViewModel.unlockLocation()
        }
    }

    override fun onScanReceived(scanValue: String) {
        shipmentInboundViewModel.submitTracking(scanValue)
    }

    private fun showUnknownShipmentDialog(trackingNo: String) {
        MaterialAlertDialogBuilder(requireContext())
            .setTitle(getString(R.string.message_unknown_shipment_title, trackingNo))
            .setMessage(getString(R.string.message_unknown_shipment))
            .setNegativeButton(R.string.action_cancel) { _, _ ->
                shipmentInboundViewModel.cancelUnknownShipment()
            }
            .setPositiveButton(R.string.action_confirm) { _, _ ->
                shipmentInboundViewModel.confirmUnknownShipment()
            }
            .show()
    }

    override fun onDestroyView() {
        _binding = null
        super.onDestroyView()
    }
}