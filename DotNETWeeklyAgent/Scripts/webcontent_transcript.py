# pyinstaller --onefile webcontent_transcript.py --collect-data newspaper  --collect-data tldextract

from newspaper import Article
import sys

if len(sys.argv) < 2:
    print("Usage: python your_script.py <input>")
    sys.exit(1)

sys.stdin.reconfigure(encoding="utf-8", errors="replace")
sys.stdout.reconfigure(encoding="utf-8", errors="replace")
sys.stderr.reconfigure(encoding="utf-8", errors="replace")

article = Article(sys.argv[1])
article.download()
article.parse()

print(article.text)