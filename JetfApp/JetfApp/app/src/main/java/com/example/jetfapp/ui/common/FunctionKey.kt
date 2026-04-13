package com.example.jetfapp.ui.common

enum class FunctionKey {
    F3,
    F4
}

interface FunctionKeyHandler {
    fun onFunctionKeyPressed(functionKey: FunctionKey)
}

interface ScanInputHandler {
    fun onScanReceived(scanValue: String)
}

interface KeyboardWedgeScanHandler : ScanInputHandler {
    fun shouldConsumeWedgeInput(): Boolean
}