/// <reference path="./jquery-3.1.1.min.js" />

var App = App || {};

App.site = (function () {
  "use strict";

  $(document).ready(function () {
    $("#hamburger").click(function (e) {
      if ($("nav.page").hasClass("visible")) $("nav.page").removeClass("visible");
      else $("nav.page").addClass("visible");
      e.preventDefault();
    });
  });
})();
