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
import com.example.jetfapp.databinding.FragmentBatchLocationTransferBinding
import com.example.jetfapp.ui.common.FunctionKey
import com.example.jetfapp.ui.common.FunctionKeyHandler
import com.example.jetfapp.ui.common.KeyboardWedgeScanHandler
import com.example.jetfapp.ui.common.hideKeyboard
import com.example.jetfapp.viewmodel.AppViewModel
import com.example.jetfapp.viewmodel.BatchLocationTransferEvent
import com.example.jetfapp.viewmodel.BatchLocationTransferViewModel
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import kotlinx.coroutines.launch

class BatchLocationTransferFragment : Fragment(), FunctionKeyHandler, KeyboardWedgeScanHandler {
    private var _binding: FragmentBatchLocationTransferBinding? = null
    private var isApplyingUiState = false
    private var hasInitializedFocus = false
    private val binding: FragmentBatchLocationTransferBinding
        get() = checkNotNull(_binding)

    private val batchLocationTransferViewModel: BatchLocationTransferViewModel by activityViewModels {
        BatchLocationTransferViewModel.factory()
    }
    private val appViewModel: AppViewModel by activityViewModels {
        AppViewModel.factory()
    }

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentBatchLocationTransferBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        batchLocationTransferViewModel.updateEditUser(appViewModel.currentAccount.value.orEmpty())
        binding.root.isFocusable = true
        binding.root.isFocusableInTouchMode = true
        binding.editOldLocation.showSoftInputOnFocus = false
        binding.editNewLocation.showSoftInputOnFocus = false

        binding.root.post {
            focusOldLocationField()
        }

        binding.editOldLocation.doAfterTextChanged { editable ->
            if (isApplyingUiState) {
                return@doAfterTextChanged
            }
            batchLocationTransferViewModel.updateOldLocationCode(editable?.toString().orEmpty())
        }
        binding.editOldLocation.setOnFocusChangeListener { _, hasFocus ->
            syncBottomActionBarSuppressed()
            if (hasFocus) {
                binding.editOldLocation.selectAll()
            }
        }
        binding.editOldLocation.setOnClickListener {
            binding.editOldLocation.selectAll()
        }
        binding.editOldLocation.setOnEditorActionListener { _, actionId, event ->
            val isImeDone = actionId == EditorInfo.IME_ACTION_DONE
            val isImeNext = actionId == EditorInfo.IME_ACTION_NEXT
            val isEnterKey = event?.keyCode == KeyEvent.KEYCODE_ENTER
            if (isImeDone || isImeNext || isEnterKey) {
                focusNewLocationField()
                true
            } else {
                false
            }
        }

        binding.editNewLocation.doAfterTextChanged { editable ->
            if (isApplyingUiState) {
                return@doAfterTextChanged
            }
            batchLocationTransferViewModel.updateNewLocationCode(editable?.toString().orEmpty())
        }
        binding.editNewLocation.setOnFocusChangeListener { _, hasFocus ->
            syncBottomActionBarSuppressed()
            if (hasFocus) {
                binding.editNewLocation.selectAll()
            }
        }
        binding.editNewLocation.setOnClickListener {
            binding.editNewLocation.selectAll()
        }
        binding.editNewLocation.setOnEditorActionListener { _, actionId, event ->
            val isImeDone = actionId == EditorInfo.IME_ACTION_DONE
            val isEnterKey = event?.keyCode == KeyEvent.KEYCODE_ENTER
            if (isImeDone || isEnterKey) {
                batchLocationTransferViewModel.requestPreview(
                    binding.editOldLocation.text?.toString().orEmpty(),
                    binding.editNewLocation.text?.toString().orEmpty()
                )
                true
            } else {
                false
            }
        }

        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch {
                    batchLocationTransferViewModel.uiState.collect { state ->
                        isApplyingUiState = true
                        try {
                            if (binding.editOldLocation.text?.toString() != state.oldLocationCode) {
                                binding.editOldLocation.setText(state.oldLocationCode)
                                binding.editOldLocation.setSelection(state.oldLocationCode.length)
                            }
                            if (binding.editNewLocation.text?.toString() != state.newLocationCode) {
                                binding.editNewLocation.setText(state.newLocationCode)
                                binding.editNewLocation.setSelection(state.newLocationCode.length)
                            }

                            binding.editOldLocation.isEnabled = !state.isSubmitting
                            binding.editNewLocation.isEnabled = !state.isSubmitting
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
                                    focusOldLocationField()
                                }
                            }
                        } finally {
                            isApplyingUiState = false
                        }
                    }
                }

                launch {
                    batchLocationTransferViewModel.events.collect { event ->
                        when (event) {
                            BatchLocationTransferEvent.NavigateBack -> requireActivity().onBackPressedDispatcher.onBackPressed()
                            BatchLocationTransferEvent.FocusOldLocation -> binding.root.post { focusOldLocationField() }
                            BatchLocationTransferEvent.FocusNewLocation -> binding.root.post { focusNewLocationField() }
                            is BatchLocationTransferEvent.ShowConfirmationDialog -> {
                                showConfirmationDialog(event.message, event.canConfirm)
                            }
                        }
                    }
                }
            }
        }
    }

    override fun onFunctionKeyPressed(functionKey: FunctionKey) {
        when (functionKey) {
            FunctionKey.F3 -> batchLocationTransferViewModel.returnToMenu()
            FunctionKey.F4 -> batchLocationTransferViewModel.requestPreview()
        }
    }

    override fun onScanReceived(scanValue: String) {
        if (batchLocationTransferViewModel.uiState.value.isSubmitting) {
            return
        }

        val normalized = scanValue.trim().uppercase()
        val state = batchLocationTransferViewModel.uiState.value
        if (binding.editOldLocation.hasFocus() || state.oldLocationCode.isBlank()) {
            binding.editOldLocation.setText(normalized)
            binding.editOldLocation.setSelection(normalized.length)
            focusNewLocationField()
        } else {
            binding.editNewLocation.setText(normalized)
            binding.editNewLocation.setSelection(normalized.length)
            batchLocationTransferViewModel.requestPreview(state.oldLocationCode, normalized)
        }
    }

    override fun shouldConsumeWedgeInput(): Boolean = !batchLocationTransferViewModel.uiState.value.isSubmitting

    override fun onDestroyView() {
        (activity as? MainActivity)?.setBottomActionBarSuppressed(false)
        _binding = null
        super.onDestroyView()
    }

    private fun focusOldLocationField() {
        binding.editOldLocation.requestFocus()
        binding.editOldLocation.selectAll()
        syncBottomActionBarSuppressed()
        hideKeyboard(binding.root)
    }

    private fun focusNewLocationField() {
        binding.editNewLocation.requestFocus()
        binding.editNewLocation.selectAll()
        syncBottomActionBarSuppressed()
        hideKeyboard(binding.root)
    }

    private fun syncBottomActionBarSuppressed() {
        (activity as? MainActivity)?.setBottomActionBarSuppressed(false)
    }

    private fun showConfirmationDialog(message: String, canConfirm: Boolean) {
        val dialogBuilder = MaterialAlertDialogBuilder(requireContext())
            .setTitle(R.string.label_location_transfer_title)
            .setMessage(message)

        if (canConfirm) {
            dialogBuilder
                .setNegativeButton(R.string.action_cancel, null)
                .setPositiveButton(R.string.action_confirm) { _, _ ->
                    batchLocationTransferViewModel.confirmBatchUpdate()
                }
        } else {
            dialogBuilder.setPositiveButton(R.string.action_confirm) { _, _ ->
                binding.root.post {
                    if (batchLocationTransferViewModel.uiState.value.oldLocationCode.isBlank()) {
                        focusOldLocationField()
                    } else {
                        focusNewLocationField()
                    }
                }
            }
        }

        val dialog = dialogBuilder.show()
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

    private fun String?.isErrorMessage(): Boolean {
        val message = this?.trim().orEmpty()
        if (message.isBlank()) {
            return false
        }

        return !message.startsWith("整板儲位調撥成功")
    }
}
