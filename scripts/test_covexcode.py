#!/usr/bin/env python3
"""
SmartStock CovexCode Verification & Token Usage Script
Run this script to verify your CovexCode API Key and register token usage on the backend.

Usage:
    python scripts/test_covexcode.py <YOUR_COVEXCODE_API_KEY>
    or
    python scripts/test_covexcode.py  (reads from COVEXCODE_API_KEY env or appsettings.json)
"""

import sys
import os
import json
import urllib.request
import urllib.error

ENDPOINT = os.getenv("COVEXCODE_ENDPOINT", "https://llm.covexcode.com/v1")
MODEL = os.getenv("COVEXCODE_MODEL", "deepseek-v4-flash")

def get_api_key():
    # 1. From CLI argument
    if len(sys.argv) > 1 and sys.argv[1].strip():
        return sys.argv[1].strip()
    
    # 2. From Environment Variable
    env_key = os.getenv("COVEXCODE_API_KEY")
    if env_key and env_key.strip():
        return env_key.strip()
    
    # 3. From appsettings.json
    try:
        appsettings_path = os.path.join(
            os.path.dirname(__file__), "..", "backend", "src", "SmartStock.API", "appsettings.json"
        )
        if os.path.exists(appsettings_path):
            with open(appsettings_path, "r", encoding="utf-8") as f:
                data = json.load(f)
                key = data.get("CovexCode", {}).get("ApiKey")
                if key and not key.startswith("your_"):
                    return key.strip()
    except Exception:
        pass
        
    return None

def main():
    api_key = get_api_key()
    if not api_key:
        print("[!] No API key provided.")
        print("Usage: python scripts/test_covexcode.py bx-live-YOUR_KEY_HERE")
        sys.exit(1)

    masked_key = api_key[:12] + "..." + api_key[-4:] if len(api_key) > 16 else api_key
    print(f"\n[*] Testing SmartStock AI with CovexCode...")
    print(f"    - Endpoint : {ENDPOINT}")
    print(f"    - Model    : {MODEL}")
    print(f"    - Key      : {masked_key}\n")

    url = f"{ENDPOINT.rstrip('/')}/chat/completions"
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {api_key}"
    }

    payload = {
        "model": MODEL,
        "messages": [
            {
                "role": "system",
                "content": "You are SmartStock AI, an intelligent retail assistant."
            },
            {
                "role": "user",
                "content": "Give 2 tips for small Indian Kirana stores to manage fast-moving stock efficiently."
            }
        ],
        "temperature": 0.3,
        "max_tokens": 150
    }

    try:
        req = urllib.request.Request(url, data=json.dumps(payload).encode("utf-8"), headers=headers)
        with urllib.request.urlopen(req) as resp:
            res_data = json.loads(resp.read().decode("utf-8"))
            
            msg = res_data.get("choices", [{}])[0].get("message", {})
            content = msg.get("content") or msg.get("reasoning_content") or ""
            usage = res_data.get("usage", {})
            total_tokens = usage.get("total_tokens", "N/A")

            print("[+] SUCCESS! Response received from CovexCode:\n")
            print(content.strip()[:300] + ("..." if len(content.strip()) > 300 else ""))
            print(f"\n[+] Total Tokens Consumed: {total_tokens}")
            print("[+] Your CovexCode token usage has been recorded on the backend!")
            print("--> Refresh https://covexcode.com/ to see your updated 'SPENT' balance.\n")

    except urllib.error.HTTPError as e:
        error_body = e.read().decode("utf-8")
        print(f"[-] HTTP Error {e.code}: {error_body}")
        sys.exit(1)
    except Exception as e:
        print(f"[-] Error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()
