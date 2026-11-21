package com.example.jetfapp.ui.unpacking

import android.os.Bundle
import android.view.KeyEvent
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.view.inputmethod.EditorInfo
import android.widget.ArrayAdapter
import androidx.activity.OnBackPressedCallback
import androidx.core.view.isVisible
import androidx.core.widget.doAfterTextChanged
import androidx.fragment.app.Fragment
import androidx.fragment.app.activityViewModels
import androidx.fragment.app.viewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import com.example.jetfapp.MainActivity
import com.example.jetfapp.R
import com.example.jetfapp.databinding.FragmentUnpackingBinding
import com.example.jetfapp.ui.common.FunctionKey
import com.example.jetfapp.ui.common.FunctionKeyHandler
import com.example.jetfapp.ui.common.KeyboardWedgeScanHandler
import com.example.jetfapp.ui.common.hideKeyboard
import com.example.jetfapp.viewmodel.AppViewModel
import com.example.jetfapp.viewmodel.UnpackingEvent
import com.example.jetfapp.viewmodel.UnpackingMessageType
import com.example.jetfapp.viewmodel.UnpackingViewModel
import kotlinx.coroutines.launch

class UnpackingFragment : Fragment(), FunctionKeyHandler, KeyboardWedgeScanHandler {
    private var _binding: FragmentUnpackingBinding? = null
    private val binding: FragmentUnpackingBinding
        get() = checkNotNull(_binding)

    private var isApplyingUiState = false
    private var hasAutoOpenedDropdown = false
    private var isLeaving = false

    private val unpackingViewModel: UnpackingViewModel by viewModels { UnpackingViewModel.factory() }
    private val appViewModel: AppViewModel by activityViewModels { AppViewModel.factory() }

    private val backPressedCallback = object : OnBackPressedCallback(true) {
        override fun handleOnBackPressed() {
            unpackingViewModel.onBackRequested()
        }
    }

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentUnpackingBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        requireActivity().onBackPressedDispatcher.addCallback(viewLifecycleOwner, backPressedCallback)
        unpackingViewModel.updateUploadOpe(appViewModel.currentAccount.value.orEmpty())

        binding.root.isFocusable = true
        binding.root.isFocusableInTouchMode = true
        binding.dropdownDataType.keyListener = null
        binding.dropdownDataType.isCursorVisible = false
        binding.dropdownDataType.showSoftInputOnFocus = false
        binding.editBagNo.showSoftInputOnFocus = false
        binding.editTrackingNo.showSoftInputOnFocus = false

        binding.dropdownDataType.setAdapter(
            ArrayAdapter(
                requireContext(),
                android.R.layout.simple_list_item_1,
                unpackingViewModel.uiState.value.dataTypes
            )
        )
        binding.dropdownDataType.setOnClickListener {
            binding.dropdownDataType.showDropDown()
        }
        binding.dropdownDataType.setOnFocusChangeListener { _, hasFocus ->
            if (hasFocus) {
                hideKeyboard(binding.root)
            }
            syncBottomActionBarSuppressed()
        }
        // 尚未選擇作業地區時，下拉選單被關閉（點外面、返回鍵）就重新開啟，強制先選擇
        binding.dropdownDataType.setOnDismissListener {
            binding.root.post {
                if (isLeaving || _binding == null || !isResumed) {
                    return@post
                }
                if (unpackingViewModel.uiState.value.selectedDataType.isBlank()) {
                    focusDataTypeField()
                }
            }
        }
        binding.dropdownDataType.setOnItemClickListener { _, _, position, _ ->
            val selected = binding.dropdownDataType.adapter.getItem(position)?.toString().orEmpty()
            unpackingViewModel.selectDataType(selected)
        }

        binding.editBagNo.doAfterTextChanged { editable ->
            if (isApplyingUiState) {
                return@doAfterTextChanged
            }
            unpackingViewModel.updateBagNo(editable?.toString().orEmpty())
        }
        binding.editBagNo.setOnFocusChangeListener { _, hasFocus ->
            syncBottomActionBarSuppressed()
            if (hasFocus) {
                binding.editBagNo.selectAll()
            }
        }
        binding.editBagNo.setOnClickListener {
            binding.editBagNo.selectAll()
        }
        binding.editBagNo.setOnEditorActionListener { _, actionId, event ->
            handleEditorAction(actionId, event) {
                unpackingViewModel.submitBagNo(binding.editBagNo.text?.toString().orEmpty())
            }
        }

        binding.editTrackingNo.doAfterTextChanged { editable ->
            if (isApplyingUiState) {
                return@doAfterTextChanged
            }
            unpackingViewModel.updateTrackingNo(editable?.toString().orEmpty())
        }
        binding.editTrackingNo.setOnFocusChangeListener { _, hasFocus ->
            syncBottomActionBarSuppressed()
            if (hasFocus) {
                binding.editTrackingNo.selectAll()
            }
        }
        binding.editTrackingNo.setOnClickListener {
            binding.editTrackingNo.selectAll()
        }
        binding.editTrackingNo.setOnEditorActionListener { _, actionId, event ->
            handleEditorAction(actionId, event) {
                unpackingViewModel.submitTrackingNo(binding.editTrackingNo.text?.toString().orEmpty())
            }
        }

        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch {
                    unpackingViewModel.uiState.collect { state ->
                        isApplyingUiState = true
                        try {
                            if (binding.dropdownDataType.text?.toString() != state.selectedDataType) {
                                binding.dropdownDataType.setText(state.selectedDataType, false)
                            }
                            if (binding.editBagNo.text?.toString() != state.bagNo) {
                                binding.editBagNo.setText(state.bagNo)
                                binding.editBagNo.setSelection(state.bagNo.length)
                            }
                            if (binding.editTrackingNo.text?.toString() != state.trackingNo) {
                                binding.editTrackingNo.setText(state.trackingNo)
                                binding.editTrackingNo.setSelection(state.trackingNo.length)
                            }

                            binding.dropdownDataType.isEnabled = !state.isBagLocked && !state.isSubmitting
                            binding.editBagNo.isEnabled = !state.isBagLocked && !state.isSubmitting
                            binding.editTrackingNo.isEnabled = state.isBagLocked && !state.isSubmitting
                            binding.loadingContainer.isVisible = state.isSubmitting

                            binding.textMessage.text = state.message.orEmpty()
                            binding.textMessage.setTextColor(
                                requireContext().getColor(
                                    when (state.messageType) {
                                        UnpackingMessageType.Success -> R.color.jetf_success
                                        UnpackingMessageType.Error -> R.color.jetf_error
                                        UnpackingMessageType.Info -> R.color.jetf_ink
                                    }
                                )
                            )

                            binding.textContentTitle.text = when {
                                state.isBagLocked -> getString(
                                    R.string.label_unpacking_pending_title,
                                    state.pendingTrackingNos.size
                                )
                                state.pendingTrackingNos.isNotEmpty() -> getString(
                                    R.string.label_unpacking_unfinished_title,
                                    state.pendingTrackingNos.size
                                )
                                else -> getString(R.string.label_unpacking_content_title)
                            }
                            binding.textContent.text = state.pendingTrackingNos.joinToString(separator = "\n")
                            binding.textContent.setTextColor(
                                requireContext().getColor(
                                    if (state.isBackConfirmPending || (!state.isBagLocked && state.pendingTrackingNos.isNotEmpty())) {
                                        R.color.jetf_error
                                    } else {
                                        R.color.jetf_ink
                                    }
                                )
                            )

                            if (!hasAutoOpenedDropdown) {
                                hasAutoOpenedDropdown = true
                                binding.root.post {
                                    if (state.selectedDataType.isBlank()) {
                                        focusDataTypeField()
                                    } else if (state.isBagLocked) {
                                        focusTrackingNoField()
                                    } else {
                                        focusBagNoField()
                                    }
                                }
                            }
                        } finally {
                            isApplyingUiState = false
                        }
                    }
                }

                launch {
                    unpackingViewModel.events.collect { event ->
                        when (event) {
                            UnpackingEvent.NavigateBack -> {
                                isLeaving = true
                                _binding?.dropdownDataType?.dismissDropDown()
                                backPressedCallback.isEnabled = false
                                requireActivity().onBackPressedDispatcher.onBackPressed()
                            }
                            UnpackingEvent.FocusDataType -> binding.root.post { focusDataTypeField() }
                            UnpackingEvent.FocusBagNo -> binding.root.post { focusBagNoField() }
                            UnpackingEvent.FocusTrackingNo -> binding.root.post { focusTrackingNoField() }
                        }
                    }
                }
            }
        }
    }

    override fun onFunctionKeyPressed(functionKey: FunctionKey) {
        when (functionKey) {
            FunctionKey.F3 -> unpackingViewModel.onBackRequested()
            FunctionKey.F4 -> Unit
        }
    }

    override fun onScanReceived(scanValue: String) {
        val state = unpackingViewModel.uiState.value
        if (state.isSubmitting) {
            return
        }

        val normalized = scanValue.trim().uppercase()
        if (normalized.isEmpty()) {
            return
        }

        if (!state.isBagLocked) {
            binding.editBagNo.setText(normalized)
            binding.editBagNo.setSelection(normalized.length)
            unpackingViewModel.submitBagNo(normalized)
        } else {
            binding.editTrackingNo.setText(normalized)
            binding.editTrackingNo.setSelection(normalized.length)
            unpackingViewModel.submitTrackingNo(normalized)
        }
    }

    override fun shouldConsumeWedgeInput(): Boolean = !unpackingViewModel.uiState.value.isSubmitting

    override fun onDestroyView() {
        (activity as? MainActivity)?.setBottomActionBarSuppressed(false)
        hasAutoOpenedDropdown = false
        _binding = null
        super.onDestroyView()
    }

    private fun handleEditorAction(actionId: Int, event: KeyEvent?, submit: () -> Unit): Boolean {
        val isImeDone = actionId == EditorInfo.IME_ACTION_DONE
        val isImeNext = actionId == EditorInfo.IME_ACTION_NEXT
        val isEnterKey = event?.keyCode == KeyEvent.KEYCODE_ENTER
        if (!isImeDone && !isImeNext && !isEnterKey) {
            return false
        }

        if (event == null || event.action == KeyEvent.ACTION_DOWN) {
            submit()
        }
        return true
    }

    private fun focusDataTypeField() {
        val currentBinding = _binding ?: return
        currentBinding.dropdownDataType.requestFocus()
        currentBinding.dropdownDataType.showDropDown()
        syncBottomActionBarSuppressed()
        hideKeyboard(currentBinding.root)
    }

    private fun focusBagNoField() {
        val currentBinding = _binding ?: return
        currentBinding.editBagNo.requestFocus()
        currentBinding.editBagNo.selectAll()
        syncBottomActionBarSuppressed()
        hideKeyboard(currentBinding.root)
    }

    private fun focusTrackingNoField() {
        val currentBinding = _binding ?: return
        currentBinding.editTrackingNo.requestFocus()
        currentBinding.editTrackingNo.selectAll()
        syncBottomActionBarSuppressed()
        hideKeyboard(currentBinding.root)
    }

    private fun syncBottomActionBarSuppressed() {
        (activity as? MainActivity)?.setBottomActionBarSuppressed(false)
    }
}
