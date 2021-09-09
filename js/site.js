 function getDimensions() {
    return {
        width: window.innerWidth,
        height: window.innerHeight
    };
};

//let browserResize = {
//    getInnerHeight: function () {
//        return window.innerHeight;
//    },
//    getInnerWidth: function () {
//        return window.innerWidth;
//    },
//    registerResizeCallback: function () {
//        window.addEventListener("resize", browserResize.resized);
//    },
//    resized: function () {
//        //DotNet.invokeMethod("BrowserResize", 'OnBrowserResize');
//        DotNet.invokeMethodAsync("BlazorClient", 'OnBrowserResize', window.innerWidth).then(data => data);
//    }
//}

function registerResizeCallback() {
    window.addEventListener("resize", resized);
}

function getInnerHeight() {
    return window.innerHeight;
}

function getInnerWidth() {
    return window.innerWidth;
}

function resized() {
    DotNet.invokeMethodAsync("ShoppingList_WebClient", 'OnBrowserResize', window.innerWidth).then(data => data);
}

function SayHelloJS() {

    console.log("!________++++++++++++  SayHelloJS  +++++++++++=")

    $('#ButtonCollapseExample').click(function () {

        if ($(this).attr('aria-expanded') == "false") {
            $(this).text('Hide Groups')
            $(this).attr('aria-expanded', "true");
        } else {
            $(this).text('Show Groups')
            $(this).attr('aria-expanded', "false");

        }


    });


    $('#ButtonCollapseExample1').click(function () {

        console.log("!________+++++++++++++++++++++++=")
        console.log($(this))
        console.log($(this).attr('aria-expanded') == "false")
      

        if ($(this).attr('aria-expanded') == "false") {
            $(this).text('Hide Lists')
            $(this).attr('aria-expanded', "true");
        } else {
            $(this).text('Show Lists')
            $(this).attr('aria-expanded', "false");

        }
    });

    var firstBigRender = true;
    var firstSmallRender = true;

    $(window).on('resize', function () {
        var win = $(this); //this = window
        if (win.width() < 992) {


            firstBigRender = true;

            if (firstSmallRender) {
                $('#collapseExample1').attr("class", "collapse");
                $('#collapseExample').attr("class", "collapse");
                firstSmallRender = false;
                firstBigRender = true;

                $('#ButtonCollapseExample1').text('Show Lists')
                $('#ButtonCollapseExample1').attr('aria-expanded', "flase");

                $('#ButtonCollapseExample').text('Show Groups')
                $('#ButtonCollapseExample').attr('aria-expanded', "false");

            }
        } else {

            if (firstBigRender) {
                $('#collapseExample1').attr("class", "collapse show");
                $('#collapseExample').attr("class", "collapse show");
                firstBigRender = false;
                firstSmallRender = true;


            }
        }

    });

}

function Collapse() {

    $('#collapseExample1').attr("class", "collapse");
    $('#collapseExample').attr("class", "collapse");


}