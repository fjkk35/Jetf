package com.example.jetfapp.ui.menu

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import com.example.jetfapp.MainActivity
import com.example.jetfapp.databinding.FragmentWarehouseMenuBinding
import com.example.jetfapp.ui.common.FunctionKey
import com.example.jetfapp.ui.common.FunctionKeyHandler
import com.example.jetfapp.ui.common.hideKeyboard

/**
 * 倉庫選單：入庫、異常件、儲位調撥。
 */
class WarehouseMenuFragment : Fragment(), FunctionKeyHandler {
    private var _binding: FragmentWarehouseMenuBinding? = null
    private val binding: FragmentWarehouseMenuBinding
        get() = checkNotNull(_binding)

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentWarehouseMenuBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        binding.root.requestFocus()
        hideKeyboard(binding.root)
        binding.buttonInbound.setOnClickListener {
            (activity as? MainActivity)?.showInboundSettings()
        }
        binding.buttonException.setOnClickListener {
            (activity as? MainActivity)?.showShipmentException()
        }
        binding.buttonLocationTransfer.setOnClickListener {
            (activity as? MainActivity)?.showLocationTransferMenu()
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
