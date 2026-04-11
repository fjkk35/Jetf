package com.example.jetfapp.ui.inbound

import android.os.Bundle
import android.view.KeyEvent
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.view.inputmethod.EditorInfo
import android.widget.ArrayAdapter
import androidx.core.view.isVisible
import androidx.core.widget.doAfterTextChanged
import androidx.fragment.app.Fragment
import androidx.fragment.app.activityViewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import com.example.jetfapp.MainActivity
import com.example.jetfapp.databinding.FragmentInboundSettingsBinding
import com.example.jetfapp.ui.common.FunctionKey
import com.example.jetfapp.ui.common.FunctionKeyHandler
import com.example.jetfapp.ui.common.ScanInputHandler
import com.example.jetfapp.ui.common.hideKeyboard
import com.example.jetfapp.viewmodel.ShipmentInboundEvent
import com.example.jetfapp.viewmodel.ShipmentInboundViewModel
import kotlinx.coroutines.launch

class InboundSettingsFragment : Fragment(), FunctionKeyHandler, ScanInputHandler {
    private var _binding: FragmentInboundSettingsBinding? = null
    private val binding: FragmentInboundSettingsBinding
        get() = checkNotNull(_binding)

    private val shipmentInboundViewModel: ShipmentInboundViewModel by activityViewModels {
        ShipmentInboundViewModel.factory()
    }

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentInboundSettingsBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        binding.dropdownSource.setOnItemClickListener { _, _, position, _ ->
            val selectedSource = binding.dropdownSource.adapter.getItem(position)?.toString().orEmpty()
            shipmentInboundViewModel.updateSelectedSourceName(selectedSource)
        }
        binding.editSequence.setOnFocusChangeListener { _, hasFocus ->
            if (hasFocus) {
                binding.editSequence.selectAll()
            }
        }
        binding.editSequence.setOnClickListener {
            binding.editSequence.selectAll()
        }
        binding.editSequence.setOnEditorActionListener { _, actionId, event ->
            val isImeDone = actionId == EditorInfo.IME_ACTION_DONE
            val isEnterKey = event?.keyCode == KeyEvent.KEYCODE_ENTER
            if (isImeDone || isEnterKey) {
                shipmentInboundViewModel.confirmSettings()
                true
            } else {
                false
            }
        }
        binding.editSequence.doAfterTextChanged { editable ->
            shipmentInboundViewModel.updateStartSequence(editable?.toString().orEmpty())
        }

        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch {
                    shipmentInboundViewModel.settingsState.collect { state ->
                        val sourceNames = state.sourceTypes.map { it.sourceType }
                        val adapter = ArrayAdapter(
                            requireContext(),
                            android.R.layout.simple_list_item_1,
                            sourceNames
                        )
                        binding.dropdownSource.setAdapter(adapter)

                        if (binding.dropdownSource.text?.toString() != state.selectedSourceName) {
                            binding.dropdownSource.setText(state.selectedSourceName, false)
                        }
                        if (binding.editSequence.text?.toString() != state.startSequence) {
                            binding.editSequence.setText(state.startSequence)
                            binding.editSequence.setSelection(state.startSequence.length)
                        }

                        binding.textMessage.isVisible = !state.message.isNullOrBlank()
                        binding.textMessage.text = state.message.orEmpty()

                        if (!state.message.isNullOrBlank()) {
                            binding.dropdownSource.clearFocus()
                            binding.editSequence.clearFocus()
                            hideKeyboard(binding.root)
                        }
                    }
                }

                launch {
                    shipmentInboundViewModel.events.collect { event ->
                        when (event) {
                            ShipmentInboundEvent.NavigateToMenu -> (activity as? MainActivity)?.showMenu()
                            ShipmentInboundEvent.NavigateToWork -> (activity as? MainActivity)?.showInboundWork()
                            ShipmentInboundEvent.NavigateToSettings -> Unit
                            is ShipmentInboundEvent.ShowUnknownShipmentDialog -> Unit
                        }
                    }
                }
            }
        }

        shipmentInboundViewModel.loadSourceTypes()
    }

    override fun onScanReceived(scanValue: String) {
        val normalized = scanValue.trim().uppercase()
        binding.editSequence.setText(normalized)
        binding.editSequence.setSelection(normalized.length)
        shipmentInboundViewModel.updateStartSequence(normalized)
    }

    override fun onFunctionKeyPressed(functionKey: FunctionKey) {
        when (functionKey) {
            FunctionKey.F3 -> shipmentInboundViewModel.returnToMenu()
            FunctionKey.F4 -> shipmentInboundViewModel.confirmSettings()
        }
    }

    override fun onDestroyView() {
        _binding = null
        super.onDestroyView()
    }
}