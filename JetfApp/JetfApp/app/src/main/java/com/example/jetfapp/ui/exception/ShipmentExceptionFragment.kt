package com.example.jetfapp.ui.exception

import android.Manifest
import android.content.pm.PackageManager
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.net.Uri
import android.os.Bundle
import android.util.Base64
import android.view.KeyEvent
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.view.inputmethod.EditorInfo
import android.widget.ArrayAdapter
import androidx.activity.result.contract.ActivityResultContracts
import androidx.core.content.ContextCompat
import androidx.core.content.FileProvider
import androidx.core.widget.doAfterTextChanged
import androidx.fragment.app.Fragment
import androidx.fragment.app.activityViewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import com.example.jetfapp.MainActivity
import com.example.jetfapp.R
import com.example.jetfapp.databinding.FragmentShipmentExceptionBinding
import com.example.jetfapp.ui.common.FunctionKey
import com.example.jetfapp.ui.common.FunctionKeyHandler
import com.example.jetfapp.ui.common.KeyboardWedgeScanHandler
import com.example.jetfapp.ui.common.hideKeyboard
import com.example.jetfapp.viewmodel.AppViewModel
import com.example.jetfapp.viewmodel.ShipmentExceptionEvent
import com.example.jetfapp.viewmodel.ShipmentExceptionViewModel
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import java.io.File
import kotlinx.coroutines.launch

class ShipmentExceptionFragment : Fragment(), FunctionKeyHandler, KeyboardWedgeScanHandler {
    private var _binding: FragmentShipmentExceptionBinding? = null
    private var isApplyingUiState = false
    private var currentPhotoFile: File? = null
    private var currentPhotoUri: Uri? = null
    private val binding: FragmentShipmentExceptionBinding
        get() = checkNotNull(_binding)

    private val shipmentExceptionViewModel: ShipmentExceptionViewModel by activityViewModels {
        ShipmentExceptionViewModel.factory()
    }
    private val appViewModel: AppViewModel by activityViewModels {
        AppViewModel.factory()
    }

    private val takePictureLauncher =
        registerForActivityResult(ActivityResultContracts.TakePicture()) { isSuccess ->
            if (isSuccess) {
                handleCapturedPhoto()
            } else {
                deleteCurrentPhotoFile()
            }
        }

    private val requestCameraPermissionLauncher =
        registerForActivityResult(ActivityResultContracts.RequestPermission()) { isGranted ->
            if (isGranted) {
                launchHighResolutionCamera()
            } else {
                shipmentExceptionViewModel.showMessage(getString(R.string.message_camera_permission_denied))
            }
        }

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentShipmentExceptionBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        val reasons = listOf(
            getString(R.string.option_exception_reason_water_damage),
            getString(R.string.option_exception_reason_damage),
            getString(R.string.option_exception_reason_leak),
            getString(R.string.option_exception_reason_label_issue),
            getString(R.string.option_exception_reason_destroy),
            getString(R.string.option_exception_reason_empty)
        )

        shipmentExceptionViewModel.updateUploadOperator(appViewModel.currentAccount.value.orEmpty())
        binding.root.isFocusable = true
        binding.root.isFocusableInTouchMode = true
        binding.dropdownReason.setAdapter(
            ArrayAdapter(
                requireContext(),
                android.R.layout.simple_list_item_1,
                reasons
            )
        )
        binding.dropdownReason.keyListener = null
        binding.dropdownReason.isCursorVisible = false
        binding.dropdownReason.showSoftInputOnFocus = false
        binding.dropdownReason.setOnKeyListener { _, _, _ -> true }
        binding.editSeqNo.showSoftInputOnFocus = false

        binding.root.post {
            focusSeqNoField()
        }

        binding.editSeqNo.setOnFocusChangeListener { _, hasFocus ->
            syncBottomActionBarSuppressed()
            if (hasFocus) {
                binding.editSeqNo.selectAll()
            }
        }
        binding.editSeqNo.setOnClickListener {
            binding.editSeqNo.selectAll()
        }
        binding.editSeqNo.setOnEditorActionListener { _, actionId, event ->
            val isImeDone = actionId == EditorInfo.IME_ACTION_DONE
            val isEnterKey = event?.keyCode == KeyEvent.KEYCODE_ENTER
            if (isImeDone || isEnterKey) {
                showReasonDropdown()
                true
            } else {
                false
            }
        }
        binding.editSeqNo.doAfterTextChanged { editable ->
            if (isApplyingUiState) {
                return@doAfterTextChanged
            }

            shipmentExceptionViewModel.updateSeqNo(editable?.toString().orEmpty())
        }

        binding.dropdownReason.setOnClickListener {
            binding.dropdownReason.showDropDown()
        }
        binding.dropdownReason.setOnFocusChangeListener { _, hasFocus ->
            syncBottomActionBarSuppressed()
            if (hasFocus) {
                hideKeyboard(binding.root)
            }
        }
        binding.dropdownReason.setOnItemClickListener { _, _, position, _ ->
            val reason = binding.dropdownReason.adapter.getItem(position)?.toString().orEmpty()
            shipmentExceptionViewModel.updateReason(reason)
            hideKeyboard(binding.root)
        }

        binding.buttonTakePhoto.setOnClickListener {
            openCamera()
        }
        binding.buttonDeletePhoto.setOnClickListener {
            deleteCurrentPhotoFile()
            shipmentExceptionViewModel.removePhoto()
        }

        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch {
                    shipmentExceptionViewModel.uiState.collect { state ->
                        isApplyingUiState = true
                        try {
                            if (binding.editSeqNo.text?.toString() != state.seqNo) {
                                binding.editSeqNo.setText(state.seqNo)
                                binding.editSeqNo.setSelection(state.seqNo.length)
                            }
                            if (binding.dropdownReason.text?.toString() != state.reason) {
                                binding.dropdownReason.setText(state.reason, false)
                            }

                            binding.imagePhotoPreview.setImageBitmap(state.photoPreview)
                            binding.imagePhotoPreview.visibility = if (state.photoPreview == null) View.GONE else View.VISIBLE
                            binding.buttonTakePhoto.visibility = if (state.photoPreview == null) View.VISIBLE else View.GONE
                            binding.buttonDeletePhoto.visibility = if (state.photoPreview == null) View.GONE else View.VISIBLE

                            binding.editSeqNo.isEnabled = !state.isSubmitting
                            binding.dropdownReason.isEnabled = !state.isSubmitting
                            binding.buttonTakePhoto.isEnabled = !state.isSubmitting
                            binding.buttonDeletePhoto.isEnabled = !state.isSubmitting
                            binding.loadingContainer.visibility = if (state.isSubmitting) View.VISIBLE else View.GONE
                            binding.textMessage.text = state.message.orEmpty()
                            binding.textMessage.setTextColor(
                                requireContext().getColor(
                                    if (state.message.isErrorMessage()) R.color.jetf_error else R.color.jetf_ink
                                )
                            )
                        } finally {
                            isApplyingUiState = false
                        }
                    }
                }

                launch {
                    shipmentExceptionViewModel.events.collect { event ->
                        when (event) {
                            ShipmentExceptionEvent.NavigateToMenu -> (activity as? MainActivity)?.showMenu()
                            is ShipmentExceptionEvent.ShowUploadResultDialog -> showUploadResultDialog(event.message, event.isSuccess)
                        }
                    }
                }
            }
        }
    }

    override fun onFunctionKeyPressed(functionKey: FunctionKey) {
        when (functionKey) {
            FunctionKey.F3 -> (activity as? MainActivity)?.showMenu()
            FunctionKey.F4 -> shipmentExceptionViewModel.submit()
        }
    }

    override fun onScanReceived(scanValue: String) {
        if (shipmentExceptionViewModel.uiState.value.isSubmitting) {
            return
        }

        val normalized = scanValue.trim()
        binding.dropdownReason.clearFocus()
        binding.editSeqNo.setText(normalized)
        binding.editSeqNo.setSelection(normalized.length)
        shipmentExceptionViewModel.updateSeqNo(normalized)
        showReasonDropdown()
    }

    override fun shouldConsumeWedgeInput(): Boolean = !shipmentExceptionViewModel.uiState.value.isSubmitting

    override fun onDestroyView() {
        deleteCurrentPhotoFile()
        (activity as? MainActivity)?.setBottomActionBarSuppressed(false)
        _binding = null
        super.onDestroyView()
    }

    private fun openCamera() {
        val permissionStatus = ContextCompat.checkSelfPermission(requireContext(), Manifest.permission.CAMERA)
        if (permissionStatus == PackageManager.PERMISSION_GRANTED) {
            launchHighResolutionCamera()
        } else {
            requestCameraPermissionLauncher.launch(Manifest.permission.CAMERA)
        }
    }

    private fun focusSeqNoField() {
        binding.dropdownReason.clearFocus()
        binding.editSeqNo.requestFocus()
        binding.editSeqNo.selectAll()
        syncBottomActionBarSuppressed()
        hideKeyboard(binding.root)
    }

    private fun syncBottomActionBarSuppressed() {
        (activity as? MainActivity)?.setBottomActionBarSuppressed(false)
    }

    private fun showUploadResultDialog(message: String, isSuccess: Boolean) {
        if (isSuccess) {
            deleteCurrentPhotoFile()
        }

        val dialogBuilder = MaterialAlertDialogBuilder(requireContext())
            .setTitle(R.string.label_exception_result_title)
            .setMessage(message)

        if (isSuccess) {
            dialogBuilder
                .setNegativeButton(R.string.action_continue_photo) { _, _ ->
                    shipmentExceptionViewModel.continuePhoto()
                    binding.root.post {
                        focusSeqNoField()
                    }
                }
                .setPositiveButton(R.string.action_next_item) { _, _ ->
                    shipmentExceptionViewModel.nextItem()
                    binding.root.post {
                        focusSeqNoField()
                    }
                }
        } else {
            dialogBuilder.setPositiveButton(R.string.action_confirm) { _, _ ->
                binding.root.post {
                    focusSeqNoField()
                }
            }
        }

        val dialog = dialogBuilder.show()

        dialog.getButton(androidx.appcompat.app.AlertDialog.BUTTON_NEGATIVE)?.apply {
            isFocusable = false
            isFocusableInTouchMode = false
            clearFocus()
        }
        dialog.getButton(androidx.appcompat.app.AlertDialog.BUTTON_POSITIVE)?.apply {
            isFocusable = false
            isFocusableInTouchMode = false
            clearFocus()
        }
        dialog.window?.decorView?.clearFocus()
    }

    private fun launchHighResolutionCamera() {
        val photoFile = createTempPhotoFile()
        val authority = "${requireContext().packageName}.fileprovider"
        val photoUri = FileProvider.getUriForFile(requireContext(), authority, photoFile)

        currentPhotoFile = photoFile
        currentPhotoUri = photoUri
        takePictureLauncher.launch(photoUri)
    }

    private fun createTempPhotoFile(): File {
        val photoDirectory = File(requireContext().cacheDir, "camera").apply {
            mkdirs()
        }
        return File.createTempFile("exception_", ".jpg", photoDirectory)
    }

    private fun handleCapturedPhoto() {
        val photoFile = currentPhotoFile
        if (photoFile == null || !photoFile.exists()) {
            shipmentExceptionViewModel.showMessage(getString(R.string.message_photo_read_failed))
            deleteCurrentPhotoFile()
            return
        }

        val photoPreview = decodePreviewBitmap(photoFile)
        if (photoPreview == null) {
            shipmentExceptionViewModel.showMessage(getString(R.string.message_photo_read_failed))
            deleteCurrentPhotoFile()
            return
        }

        val photoBase64 = Base64.encodeToString(photoFile.readBytes(), Base64.NO_WRAP)
        shipmentExceptionViewModel.updatePhoto(photoPreview, photoBase64)
    }

    private fun decodePreviewBitmap(photoFile: File): Bitmap? {
        val boundsOptions = BitmapFactory.Options().apply {
            inJustDecodeBounds = true
        }
        BitmapFactory.decodeFile(photoFile.absolutePath, boundsOptions)

        val previewOptions = BitmapFactory.Options().apply {
            inSampleSize = calculateInSampleSize(boundsOptions, 1280, 1280)
            inJustDecodeBounds = false
        }
        return BitmapFactory.decodeFile(photoFile.absolutePath, previewOptions)
    }

    private fun calculateInSampleSize(
        options: BitmapFactory.Options,
        reqWidth: Int,
        reqHeight: Int
    ): Int {
        val height = options.outHeight
        val width = options.outWidth
        var inSampleSize = 1

        if (height > reqHeight || width > reqWidth) {
            var halfHeight = height / 2
            var halfWidth = width / 2

            while (halfHeight / inSampleSize >= reqHeight && halfWidth / inSampleSize >= reqWidth) {
                inSampleSize *= 2
            }
        }

        return inSampleSize.coerceAtLeast(1)
    }

    private fun deleteCurrentPhotoFile() {
        currentPhotoFile?.takeIf { it.exists() }?.delete()
        currentPhotoFile = null
        currentPhotoUri = null
    }

    private fun showReasonDropdown() {
        binding.root.requestFocus()
        binding.dropdownReason.clearFocus()
        binding.dropdownReason.post {
            if (_binding != null && !shipmentExceptionViewModel.uiState.value.isSubmitting) {
                binding.dropdownReason.showDropDown()
            }
        }
    }

    private fun String?.isErrorMessage(): Boolean {
        val message = this?.trim().orEmpty()
        if (message.isBlank()) {
            return false
        }

        return !message.startsWith("異常件上傳成功")
    }
}
