package com.example.jetfapp.ui.menu

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import com.example.jetfapp.MainActivity
import com.example.jetfapp.databinding.FragmentSiteMenuBinding
import com.example.jetfapp.ui.common.FunctionKey
import com.example.jetfapp.ui.common.FunctionKeyHandler
import com.example.jetfapp.ui.common.hideKeyboard

/**
 * 現場選單：拆袋作業。
 */
class SiteMenuFragment : Fragment(), FunctionKeyHandler {
    private var _binding: FragmentSiteMenuBinding? = null
    private val binding: FragmentSiteMenuBinding
        get() = checkNotNull(_binding)

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentSiteMenuBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        binding.root.requestFocus()
        hideKeyboard(binding.root)
        binding.buttonUnpacking.setOnClickListener {
            (activity as? MainActivity)?.showUnpacking()
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
