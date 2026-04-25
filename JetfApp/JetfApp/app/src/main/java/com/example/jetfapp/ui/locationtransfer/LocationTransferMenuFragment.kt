package com.example.jetfapp.ui.locationtransfer

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import com.example.jetfapp.MainActivity
import com.example.jetfapp.databinding.FragmentLocationTransferMenuBinding
import com.example.jetfapp.ui.common.FunctionKey
import com.example.jetfapp.ui.common.FunctionKeyHandler
import com.example.jetfapp.ui.common.hideKeyboard

class LocationTransferMenuFragment : Fragment(), FunctionKeyHandler {
    private var _binding: FragmentLocationTransferMenuBinding? = null
    private val binding: FragmentLocationTransferMenuBinding
        get() = checkNotNull(_binding)

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentLocationTransferMenuBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        binding.root.requestFocus()
        hideKeyboard(binding.root)
        binding.buttonSingleLocationTransfer.setOnClickListener {
            (activity as? MainActivity)?.showSingleLocationTransfer()
        }
        binding.buttonBatchLocationTransfer.setOnClickListener {
            (activity as? MainActivity)?.showBatchLocationTransfer()
        }
    }

    override fun onFunctionKeyPressed(functionKey: FunctionKey) {
        when (functionKey) {
            FunctionKey.F3 -> requireActivity().onBackPressedDispatcher.onBackPressed()
            FunctionKey.F4 -> Unit
        }
    }

    override fun onDestroyView() {
        _binding = null
        super.onDestroyView()
    }
}
