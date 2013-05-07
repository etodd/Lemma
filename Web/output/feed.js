google.load("feeds", "1");

// Our callback function, for when a feed is loaded.
function feedLoaded(result, selector)
{
	if (!result.error)
	{
		// Grab the container we will put the results into
		var container = $(selector);
		
		var entry = result.feed.entries[0];
		
		container.html('<h3><a href="' + entry.link + '">' + entry.title + '</a></h3><small>' + entry.publishedDate + '</small><div>' + entry.content + '</div>');
		
		container.fitVids();
	}
}

function populateFeed(selector, url)
{
	// Create a feed instance that will grab our feed.
	var feed = new google.feeds.Feed(url);

	// Calling load sends the request off.  It requires a callback function.
	feed.load(function(result) { feedLoaded(result, selector); });
}