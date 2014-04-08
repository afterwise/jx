
all: Test.exe DslTest.exe

Test.exe: Test.cs Jx.cs
	mcs -define:JX_INTERNING -unsafe $^ -out:$@

DslTest.exe: DslTest.cs Jx.cs
	mcs -unsafe $^ -out:$@

.PHONY: clean
clean:
	rm -f Test.exe DslTest.exe

