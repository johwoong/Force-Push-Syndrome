#!/usr/bin/env bash
# PreToolUse(Edit|Write): 비밀/설정 파일 및 Unity 생성물 수정 차단
input=$(cat)
path=$(printf '%s' "$input" | grep -o '"file_path"[[:space:]]*:[[:space:]]*"[^"]*"' | head -1 | sed 's/.*:[[:space:]]*"//; s/"$//')
case "$path" in
  *.env|*/.env|*.env.*|*application-prod*|*application-local*|*/Library/*|*/Temp/*|*.meta)
    case "$path" in *.env.example) exit 0;; esac
    echo "차단: '$path' 는 비밀/설정/Unity 생성 파일이라 AI가 수정하지 않습니다. 필요하면 사람이 직접 수정하세요." >&2
    exit 2;;
esac
exit 0
