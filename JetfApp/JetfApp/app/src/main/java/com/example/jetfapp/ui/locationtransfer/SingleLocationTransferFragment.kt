package com.example.jetfapp.ui.locationtransfer

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
import com.example.jetfapp.databinding.FragmentSingleLocationTransferBinding
import com.example.jetfapp.ui.common.FunctionKey
import com.example.jetfapp.ui.common.FunctionKeyHandler
import com.example.jetfapp.ui.common.KeyboardWedgeScanHandler
import com.example.jetfapp.ui.common.hideKeyboard
import com.example.jetfapp.viewmodel.AppViewModel
import com.example.jetfapp.viewmodel.SingleLocationTransferEvent
import com.example.jetfapp.viewmodel.SingleLocationTransferViewModel
import kotlinx.coroutines.launch

class SingleLocationTransferFragment : Fragment(), FunctionKeyHandler, KeyboardWedgeScanHandler {
    private var _binding: FragmentSingleLocationTransferBinding? = null
    private var isApplyingUiState = false
    private var hasInitializedFocus = false
    private val binding: FragmentSingleLocationTransferBinding
        get() = checkNotNull(_binding)

    private val singleLocationTransferViewModel: SingleLocationTransferViewModel by activityViewModels {
        SingleLocationTransferViewModel.factory()
    }
    private val appViewModel: AppViewModel by activityViewModels {
        AppViewModel.factory()
    }

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentSingleLocationTransferBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        singleLocationTransferViewModel.updateEditUser(appViewModel.currentAccount.value.orEmpty())
        binding.root.isFocusable = true
        binding.root.isFocusableInTouchMode = true
        binding.editLocation.showSoftInputOnFocus = false
        binding.editSeqNo.showSoftInputOnFocus = false

        binding.root.post {
            focusLocationField()
        }

        binding.editLocation.doAfterTextChanged { editable ->
            if (isApplyingUiState) {
                return@doAfterTextChanged
            }
            singleLocationTransferViewModel.updateLocationCode(editable?.toString().orEmpty())
        }
        binding.editLocation.setOnFocusChangeListener { _, hasFocus ->
            syncBottomActionBarSuppressed()
            if (hasFocus) {
                binding.editLocation.selectAll()
            } else if (
                binding.editLocation.text?.isNotBlank() == true &&
                !singleLocationTransferViewModel.uiState.value.isLocationLocked &&
                !singleLocationTransferViewModel.uiState.value.isSubmitting
            ) {
                singleLocationTransferViewModel.lockLocation()
            }
        }
        binding.editLocation.setOnClickListener {
            binding.editLocation.selectAll()
        }
        binding.editLocation.setOnEditorActionListener { _, actionId, event ->
            val isImeDone = actionId == EditorInfo.IME_ACTION_DONE
            val isImeNext = actionId == EditorInfo.IME_ACTION_NEXT
            val isEnterKey = event?.keyCode == KeyEvent.KEYCODE_ENTER
            if (isImeDone || isImeNext || isEnterKey) {
                singleLocationTransferViewModel.lockLocation()
                true
            } else {
                false
            }
        }

        binding.editSeqNo.doAfterTextChanged { editable ->
            if (isApplyingUiState) {
                return@doAfterTextChanged
            }
            singleLocationTransferViewModel.updateSeqNo(editable?.toString().orEmpty())
        }
        binding.editSeqNo.setOnFocusChangeListener { _, hasFocus ->
            syncBottomActionBarSuppressed()
            if (hasFocus) {
                binding.editSeqNo.selectAll()
            }
        }
        binding.editSeqNo.setOnClickListener {
            binding.editSeqNo.selectAll()
        }
        binding.editSeqNo.setOnEditorActionListener { _, actionId, event ->
            val isImeDone = actionId == EditorInfo.IME_ACTION_DONE
            val isEnterKey = event?.keyCode == KeyEvent.KEYCODE_ENTER
            if (isImeDone || isEnterKey) {
                singleLocationTransferViewModel.submit(binding.editSeqNo.text?.toString().orEmpty())
                true
            } else {
                false
            }
        }

        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch {
                    singleLocationTransferViewModel.uiState.collect { state ->
                        isApplyingUiState = true
                        try {
                            if (binding.editLocation.text?.toString() != state.locationCode) {
                                binding.editLocation.setText(state.locationCode)
                                binding.editLocation.setSelection(state.locationCode.length)
                            }
                            if (binding.editSeqNo.text?.toString() != state.seqNo) {
                                binding.editSeqNo.setText(state.seqNo)
                                binding.editSeqNo.setSelection(state.seqNo.length)
                            }

                            binding.editLocation.isEnabled = !state.isLocationLocked && !state.isSubmitting
                            binding.editSeqNo.isEnabled = state.isLocationLocked && !state.isSubmitting
                            binding.loadingContainer.isVisible = state.isSubmitting
                            binding.textMessage.text = state.message.orEmpty()
                            binding.textMessage.setTextColor(
                                requireContext().getColor(
                                    if (state.message.isErrorMessage()) R.color.jetf_error else R.color.jetf_ink
                                )
                            )

                            if (!hasInitializedFocus && !state.isSubmitting) {
                                hasInitializedFocus = true
                                binding.root.post {
                                    focusLocationField()
                                }
                            }
                        } finally {
                            isApplyingUiState = false
                        }
                    }
                }

                launch {
                    singleLocationTransferViewModel.events.collect { event ->
                        when (event) {
                            SingleLocationTransferEvent.NavigateBack -> requireActivity().onBackPressedDispatcher.onBackPressed()
                            SingleLocationTransferEvent.FocusLocation -> binding.root.post { focusLocationField() }
                            SingleLocationTransferEvent.FocusSeqNo -> binding.root.post { focusSeqNoField() }
                        }
                    }
                }
            }
        }
    }

    override fun onFunctionKeyPressed(functionKey: FunctionKey) {
        when (functionKey) {
            FunctionKey.F3 -> singleLocationTransferViewModel.returnToMenu()
            FunctionKey.F4 -> singleLocationTransferViewModel.unlockLocation()
        }
    }

    override fun onScanReceived(scanValue: String) {
        if (singleLocationTransferViewModel.uiState.value.isSubmitting) {
            return
        }

        val normalized = scanValue.trim()
        val state = singleLocationTransferViewModel.uiState.value
        if (!state.isLocationLocked) {
            binding.editLocation.setText(normalized)
            binding.editLocation.setSelection(normalized.length)
            singleLocationTransferViewModel.lockLocation(normalized)
        } else {
            binding.editSeqNo.setText(normalized)
            binding.editSeqNo.setSelection(normalized.length)
            singleLocationTransferViewModel.submit(normalized)
        }
    }

    override fun shouldConsumeWedgeInput(): Boolean = !singleLocationTransferViewModel.uiState.value.isSubmitting

    override fun onDestroyView() {
        (activity as? MainActivity)?.setBottomActionBarSuppressed(false)
        _binding = null
        super.onDestroyView()
    }

    private fun focusLocationField() {
        binding.editLocation.requestFocus()
        binding.editLocation.selectAll()
        syncBottomActionBarSuppressed()
        hideKeyboard(binding.root)
    }

    private fun focusSeqNoField() {
        binding.editSeqNo.requestFocus()
        binding.editSeqNo.selectAll()
        syncBottomActionBarSuppressed()
        hideKeyboard(binding.root)
    }

    private fun syncBottomActionBarSuppressed() {
        (activity as? MainActivity)?.setBottomActionBarSuppressed(false)
    }

    private fun String?.isErrorMessage(): Boolean {
        val message = this?.trim().orEmpty()
        if (message.isBlank()) {
            return false
        }

        return !message.startsWith("儲位調撥成功") &&
            !message.startsWith("新儲位已鎖定") &&
            !message.startsWith("已解除新儲位鎖定")
    }
}
