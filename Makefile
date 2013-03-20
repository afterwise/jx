
Test.exe:
	mcs -unsafe Jx.cs Test.cs -out:$@

.PHONY: clean
clean:
	rm -f Test.exe

