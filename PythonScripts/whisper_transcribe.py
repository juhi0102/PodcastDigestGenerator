import sys
import json
import whisper

if len(sys.argv) < 2:
    print(json.dumps({ "error": "No audio file provided." }))
    sys.exit(1)

model = whisper.load_model("base")
audio = sys.argv[1]

try:
    result = model.transcribe(audio)
    print(json.dumps({ "transcript": result["text"] }))
except Exception as e:
    print(json.dumps({ "error": str(e) }))
    sys.exit(1)
