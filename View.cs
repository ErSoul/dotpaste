namespace dotpaste
{
    public class View
    {
		[Obsolete("This method returns inconsistent results. Use View.TemplateHTML()")]
        public static string Template(string fileContent)
        {
            return $@"
<!DOCTYPE html>
<html>
	<head>
		<title>dotpaste</title>
		<link href=""/css/prism.css"" rel=""stylesheet"" />
	</head>
	<body class='line-numbers'>
		<pre>
			<code id=""code-block"">
{fileContent}
			</code>
		</pre>
		<footer>
			<script src=""/js/prism.js""></script>
			<script>
				const urlParams = new URLSearchParams(window.location.search);
				const myParam = urlParams.get('lang');
				
				var codeNode = document.getElementById(""code-block"");
				
				if(myParam != undefined || myParam != null)
					codeNode.className = ""language-"" + myParam;
			</script>
		</footer>
	</body>
</html>
            ";
        }

        public static string TemplateHTML(string fileContent)
        {
            return $@"
<!DOCTYPE html>
<html>
	<head>
		<title>dotpaste</title>
		<link href=""/css/prism.css"" rel=""stylesheet"" />
	</head>
	<body class='line-numbers'>
		<script id='code-block' type=""text/plain"">{fileContent}</script>
		<footer>
			<script src=""/js/prism.js""></script>
			<script>
				const urlParams = new URLSearchParams(window.location.search);
				const myParam = urlParams.get('lang');
				
				var codeNode = document.getElementById(""code-block"");
				
				if(myParam != undefined || myParam != null)
					codeNode.className = ""language-"" + myParam;
			</script>
		</footer>
	</body>
</html>
            ";
        }
    }
}
