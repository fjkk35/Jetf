package com.example.jetfapp.ui.common

import android.content.Context
import android.util.AttributeSet
import android.view.inputmethod.EditorInfo
import android.view.inputmethod.InputConnection
import com.google.android.material.textfield.MaterialAutoCompleteTextView

class NoImeAutoCompleteTextView @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null,
    defStyleAttr: Int = androidx.appcompat.R.attr.autoCompleteTextViewStyle
) : MaterialAutoCompleteTextView(context, attrs, defStyleAttr) {

    init {
        showSoftInputOnFocus = false
    }

    override fun onCheckIsTextEditor(): Boolean = false

    override fun onCreateInputConnection(outAttrs: EditorInfo): InputConnection? = null
}