#!/usr/bin/env bash
# PreToolUse(Bash): 위험한 Git 명령 차단 + 커밋 메시지 형식 검사
input=$(cat)
cmd=$(printf '%s' "$input" | grep -o '"command"[[:space:]]*:[[:space:]]*"\(\\"\|[^"]\)*"' | head -1 | sed 's/^"command"[[:space:]]*:[[:space:]]*"//; s/"$//')
if printf '%s' "$cmd" | grep -Eq 'git push|git reset --hard|git clean -[a-z]*f|git checkout -- \.|push --force|push -f'; then
  echo "차단: push/강제 초기화 계열 Git 명령은 사람이 직접 실행합니다." >&2
  exit 2
fi
if printf '%s' "$cmd" | grep -Eq 'git commit'; then
  msg=$(printf '%s' "$cmd" | grep -o -- '-m[[:space:]]*\\\?"[^"\\]*' | head -1 | sed 's/^-m[[:space:]]*\\\?"//')
  if [ -n "$msg" ] && ! printf '%s' "$msg" | grep -Eq '^(feat|fix|docs|refactor|test|chore) ?: '; then
    echo "차단: 커밋 메시지는 'feat : 설명' 형식이어야 합니다. (feat|fix|docs|refactor|test|chore)" >&2
    exit 2
  fi
fi
exit 0
