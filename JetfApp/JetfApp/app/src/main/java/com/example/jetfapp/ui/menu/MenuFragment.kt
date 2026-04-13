package com.example.jetfapp.ui.menu

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.fragment.app.activityViewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import com.example.jetfapp.MainActivity
import com.example.jetfapp.R
import com.example.jetfapp.databinding.FragmentMenuBinding
import com.example.jetfapp.ui.common.FunctionKey
import com.example.jetfapp.ui.common.FunctionKeyHandler
import com.example.jetfapp.ui.common.hideKeyboard
import com.example.jetfapp.viewmodel.AppViewModel
import kotlinx.coroutines.launch

class MenuFragment : Fragment(), FunctionKeyHandler {
    private var _binding: FragmentMenuBinding? = null
    private val binding: FragmentMenuBinding
        get() = checkNotNull(_binding)

    private val appViewModel: AppViewModel by activityViewModels { AppViewModel.factory() }

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentMenuBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        binding.root.requestFocus()
        hideKeyboard(binding.root)
        binding.buttonInbound.setOnClickListener {
            (activity as? MainActivity)?.showInboundSettings()
        }

        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                appViewModel.currentAccount.collect { account ->
                    binding.textAccount.text = getString(
                        R.string.label_current_account,
                        account.orEmpty()
                    )
                }
            }
        }
    }

    override fun onFunctionKeyPressed(functionKey: FunctionKey) {
        when (functionKey) {
            FunctionKey.F3 -> activity?.finish()
            FunctionKey.F4 -> (activity as? MainActivity)?.showInboundSettings()
        }
    }

    override fun onDestroyView() {
        _binding = null
        super.onDestroyView()
    }
}