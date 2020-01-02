var lunr = require("lunr");
var fs = require("fs");

// Replace: because node fs is an asshole (BOM)
var dataJson = fs.readFileSync('src/lunr-data.json', 'utf8').replace(/^\uFEFF/, "");
var data = JSON.parse(dataJson);

var idx = lunr(function () {
  this.ref('id');
  this.field('subject');
  this.field('body');

  data.forEach(function (email) {
    this.add(email);
  }, this);
});

var idxJson = JSON.stringify(idx);
fs.writeFileSync("src/lunr-index.json", idxJson);

// var idx = lunr.Index.load(JSON.parse(data));
//var res = idx.search("launch");
//console.log(JSON.stringify(res));

