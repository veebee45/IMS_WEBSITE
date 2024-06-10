var swiper = new Swiper(".mySwiper", {
    loop: false,
    effect: "coverflow",
    grabCursor: true,
    centeredSlides: true,
    slidesPerView: "auto",
    initialSlide: Math.floor($(".mySwiper .swiper-slide").length / 2.5),
    coverflowEffect: {
        rotate: 0,
        stretch: 0,
        depth: 300,
        modifier: 1,
        slideShadows: false,
    },
    pagination: {
        el: ".swiper-pagination",
        clickable: true, // Enable pagination clickable
        renderBullet: function (index, className) {
            return '<span class="' + className + '"></span>'; // Custom pagination bullet
        },
    },
});


$(document).ready(function () {
    var runExeBtns = $(".card_btn");

    runExeBtns.click(function () {
        var value = $(this).data("value");

        $.ajax({
            type: "POST",
            url: '/Epic/HandleButtonClick', // Adjust the URL to match your controller route
            data: { value: value }, // Pass the value here
            success: function (response) {
                if (response.success) {
                } else {
                    alert("Execution failed: " + response.error);
                }
            },
            error: function (xhr, status, error) {
                alert("Error occurred:", error);
            }
        });
    });
});