#!/usr/bin/env bash
set -eu
cd /workspace/ui/finstance
if ! curl --silent --fail http://localhost:3000 >/dev/null; then
  nohup npm run dev > /tmp/finstance-ui.log 2>&1 < /dev/null &
fi
