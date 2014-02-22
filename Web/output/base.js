// Google analytics
var _gaq = _gaq || [];
_gaq.push(['_setAccount', 'UA-34545135-1']);
_gaq.push(['_setDomainName', 'lemmagame.com']);
_gaq.push(['_trackPageview']);

(function()
{
	var ga = document.createElement('script'); ga.type = 'text/javascript'; ga.async = true;
	ga.src = ('https:' == document.location.protocol ? 'https://' : 'http://') + 'stats.g.doubleclick.net/dc.js';
	var s = document.getElementsByTagName('script')[0]; s.parentNode.insertBefore(ga, s);
})();

// SVG fallback
if (!document.implementation.hasFeature('http://www.w3.org/TR/SVG11/feature#BasicStructure', '1.1'))
	$('header').addClass('svg-fallback');

// Fitvids
$(document).ready(function()
{
	$('article').fitVids();
});