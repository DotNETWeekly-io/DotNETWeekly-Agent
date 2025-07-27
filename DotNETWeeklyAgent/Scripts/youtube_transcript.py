from youtube_transcript_api import YouTubeTranscriptApi
import sys

if len(sys.argv) < 2:
    print("Usage: python your_script.py <input>")
    sys.exit(1)

ytt_api = YouTubeTranscriptApi()
fetched_transcript = ytt_api.fetch(sys.argv[1])

transcript = ''
for snippet in fetched_transcript:
    transcript += (' ' + snippet.text)

print(transcript)