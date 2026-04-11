package com.example.jetfapp.ui.login

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
import com.example.jetfapp.databinding.FragmentLoginBinding
import com.example.jetfapp.ui.common.FunctionKey
import com.example.jetfapp.ui.common.FunctionKeyHandler
import com.example.jetfapp.viewmodel.AppEvent
import com.example.jetfapp.viewmodel.AppViewModel
import kotlinx.coroutines.launch

class LoginFragment : Fragment(), FunctionKeyHandler {
    private var _binding: FragmentLoginBinding? = null
    private val binding: FragmentLoginBinding
        get() = checkNotNull(_binding)

    private val appViewModel: AppViewModel by activityViewModels { AppViewModel.factory() }

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentLoginBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        binding.buttonLogin.setOnClickListener {
            appViewModel.login()
        }
        binding.editAccount.setOnEditorActionListener { _, actionId, event ->
            val isImeDone = actionId == EditorInfo.IME_ACTION_DONE
            val isEnterKey = event?.keyCode == KeyEvent.KEYCODE_ENTER
            if (isImeDone || isEnterKey) {
                appViewModel.login()
                true
            } else {
                false
            }
        }
        binding.editAccount.doAfterTextChanged { editable ->
            appViewModel.updateAccount(editable?.toString().orEmpty())
        }

        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch {
                    appViewModel.loginState.collect { state ->
                        if (binding.editAccount.text?.toString() != state.account) {
                            binding.editAccount.setText(state.account)
                            binding.editAccount.setSelection(state.account.length)
                        }
                        binding.buttonLogin.isEnabled = !state.isSubmitting
                        binding.textMessage.isVisible = !state.message.isNullOrBlank()
                        binding.textMessage.text = state.message.orEmpty()
                    }
                }

                launch {
                    appViewModel.events.collect { event ->
                        if (event is AppEvent.NavigateToMenu) {
                            (activity as? MainActivity)?.showMenu()
                        }
                    }
                }
            }
        }
    }

    override fun onFunctionKeyPressed(functionKey: FunctionKey) {
        when (functionKey) {
            FunctionKey.F3 -> activity?.finish()
            FunctionKey.F4 -> appViewModel.login()
        }
    }

    override fun onDestroyView() {
        _binding = null
        super.onDestroyView()
    }
}