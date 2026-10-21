#!/usr/bin/env bash
# PostToolUse(Edit|Write): 서버 Java 파일을 고친 뒤 컴파일 오류를 바로 알려준다
input=$(cat)
path=$(printf '%s' "$input" | grep -o '"file_path"[[:space:]]*:[[:space:]]*"[^"]*"' | head -1 | sed 's/.*:[[:space:]]*"//; s/"$//')
case "$path" in
  *COU_Server*src/main/*.java|*COU_Server*src/test/*.java)
    root=$(printf '%s' "$path" | sed 's#\(.*COU_Server\)/.*#\1#')
    out=$(cd "$root" && ./gradlew compileJava compileTestJava -q --console=plain 2>&1)
    if [ $? -ne 0 ]; then
      echo "컴파일 실패 — 수정한 코드를 확인하세요:" >&2
      printf '%s\n' "$out" | tail -25 >&2
      exit 2
    fi;;
esac
exit 0
