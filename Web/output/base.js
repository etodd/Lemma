// Google analytics
var _gaq = _gaq || [];
_gaq.push(['_setAccount', 'UA-34545135-1']);
_gaq.push(['_setDomainName', 'lemmagame.com']);
_gaq.push(['_trackPageview']);

(function()
{
	var ga = document.createElement('script'); ga.type = 'text/javascript'; ga.async = true;
	ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js';
	var s = document.getElementsByTagName('script')[0]; s.parentNode.insertBefore(ga, s);
})();

// SVG fallback
if (!document.implementation.hasFeature('http://www.w3.org/TR/SVG11/feature#BasicStructure', '1.1'))
	$('header').addClass('svg-fallback');

// Facebook
(function(d, s, id)
{
	var js, fjs = d.getElementsByTagName(s)[0];
	if (d.getElementById(id)) return;
	js = d.createElement(s); js.id = id;
	js.src = "//connect.facebook.net/en_US/all.js#xfbml=1&appId=254751214540576";
	fjs.parentNode.insertBefore(js, fjs);
}(document, 'script', 'facebook-jssdk'));

// Fitvids
$(document).ready(function()
{
	$('article').fitVids();
});