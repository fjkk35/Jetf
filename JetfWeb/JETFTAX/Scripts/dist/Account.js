$(document).ready(function () {
    //登入
    $("#loginForm").submit(function (e) {
        //防止連點
        $("#loginBtn").attr('disabled', true);

        let form = $(this);
        let url = Router.action('Account', 'Login');
        //let url = form.attr('action');

        $.ajax({
            type: "POST",
            url: url,
            data: form.serialize(), // serializes the form's elements.
            success: function (data) {
                if (data.status == "success") {
                    swal({
                        title: data.msg,
                        icon: "success",
                        timer: 2000,
                        showConfirmButton: false, // There won't be any confirm button
                    }).then(function () {
                        //成功導回首頁
                        location.href = Router.action('SeaTaxUpload', 'Index');
                        //location.href = "/Home/Index";
                    });
                }
                else {
                    //顯示錯誤資訊
                    swal({
                        title: data.msg,
                        icon: "error"
                    });

                    //有問題就重刷驗證碼
                    let i = Math.random();
                    $("#codeLogin").attr("src", Router.action('Captcha', 'GetValidateCode') + "?key=Login&i=" + i);

                    $("#loginBtn").attr('disabled', false);
                }
            }
        });

        e.preventDefault(); // avoid to execute the actual submit of the form.
    });
});


//刷新驗證碼
let ReflashCaptcha = function (v) {
        let i = Math.random();
        let url = Router.action('Captcha', 'GetValidateCode');
        $("#code" + v).attr("src", url + "?key=" + v + "&i=" + i);
};
