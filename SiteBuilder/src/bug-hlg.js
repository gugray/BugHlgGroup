/// <reference path="./jquery-3.1.1.min.js" />

var App = App || {};

App.site = (function () {
  "use strict";

  $(document).ready(function () {
    // Hamburger menu (needed for mobile view)
    $("#hamburger").click(function (e) {
      if ($("nav.page").hasClass("visible")) $("nav.page").removeClass("visible");
      else $("nav.page").addClass("visible");
      e.preventDefault();
    });
    // Only load search data where needed
    if ($("#txtSearch").length != 0) {
      // Search index and data
      $.getJSON("/lunr-index.json", (data) => {
        App.idx = lunr.Index.load(data);
        if (App.idx && App.msgs) initSearchEvents();
      });
      $.getJSON("/lunr-data.json", (data) => {
        App.msgs = data;
        if (App.idx && App.msgs) initSearchEvents();
      });
    }
  });

  function initSearchEvents() {
    App.origListHtml = $("section.page ul.items").html();
    $("#btnSearch").click(onSearch);
    $("#txtSearch").keyup(function (e) {
      if (e.keyCode == 13) {
        onSearch();
        return false;
      }
    });
    $("#btnClear").click(function (e) {
      $("#txtSearch").val("");
      onSearch();
    });
  }

  const itmHtml = '<li>' +
    '<a href="{{msgLink}}" class="subject">{{subject}}</a>' +
    '<p class="snippet">{{snippet}}</p>' +
    '<p class="details">From <span class="author">{{author}}</span> · {{date}}</p>' +
    '</li>';

  function onSearch() {
    var query = $("#txtSearch").val().trim();
    if (query == "") {
      resetPage();
      return;
    }
    var res = App.idx.search(query);
    $("header nav").addClass("hidden");
    $("footer nav").addClass("hidden");
    $("#btnClear").addClass("visible");
    var ul = $("section.page ul.items");
    ul.html("");
    if (res.length == 0) {

    }
    else {
      for (var i = 0; i < res.length && i < 100; ++i) {
        var itm = res[i];
        var terms = Object.getOwnPropertyNames(itm.matchData.metadata);
        var msg = null;
        for (var j = 0; j < App.msgs.length; ++j) {
          if (App.msgs[j].id == itm.ref) {
            msg = App.msgs[j];
            break;
          }
        }
        var html = itmHtml.replace("{{msgLink}}", "/messages/" + msg.id);
        html = html.replace("{{date}}", msg.date);
        var elm = $(html);
        elm.find(".subject").text(msg.subject);
        elm.find(".snippet").text(getSnippet(terms, msg));
        elm.find(".author").text(msg.authorName);
        // Highlights
        var mark = new Mark(elm);
        for (var j = 0; j < terms.length; ++j) {
          mark.mark(terms[j]);
        }
        ul.append(elm);
      }
    }
  }

  function resetPage() {
    $("section.page ul.items").html(App.origListHtml);
    $("header nav").removeClass("hidden");
    $("footer nav").removeClass("hidden");
    $("#btnClear").removeClass("visible");
  }

  function getSnippet(terms, msg) {
    var firstIndex = -1;
    for (var i = 0; i < terms.length; ++i) {
      var term = terms[i];
      var ix = msg.body.indexOf(term);
      if (ix == -1) continue;
      if (firstIndex == -1 || ix < firstIndex) firstIndex = ix;
    }
    // Term not found: snippet is first 150 chars of message
    if (firstIndex == -1)
      return msg.body.substring(0, 150);
    // We want a windows of about 70 before & after
    var startIx = firstIndex - 70;
    if (startIx < 0) startIx = 0;
    return msg.body.substr(startIx, 150);
  }
})();
