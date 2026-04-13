package com.example.jetfapp.ui.common

import android.content.Context
import android.view.View
import android.view.inputmethod.InputMethodManager
import androidx.fragment.app.Fragment

fun Fragment.hideKeyboard(targetView: View) {
    val inputMethodManager = requireContext().getSystemService(Context.INPUT_METHOD_SERVICE) as? InputMethodManager
    inputMethodManager?.hideSoftInputFromWindow(targetView.windowToken, 0)
}

fun Fragment.showKeyboard(targetView: View) {
    val inputMethodManager = requireContext().getSystemService(Context.INPUT_METHOD_SERVICE) as? InputMethodManager
    targetView.requestFocus()
    targetView.post {
        inputMethodManager?.showSoftInput(targetView, InputMethodManager.SHOW_IMPLICIT)
    }
}