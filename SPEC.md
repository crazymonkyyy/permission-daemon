
## GOAL

produce an deamon to prevent ai's from deleting files using preexisting unix tools. For example consider an ai running as "editor" and an ai running as "tester", editor MUST not delete tests, this could be expressed simply enough with a regex of test files and a unix premision.

Write up a file format that has pairs of string "patterns" with unix file preimssions, such a file format should be easy for humans who havnt read the spec to attempt to modify.

Produce a deamon that watches this file, and watches the folder its running in.

## patterns

Consider that ai agents have practice writing `.gitignore`, regex. I prefer `glob`. Do not roll your own code, use off the shelf libs and use the tests together so whatever the user types just works

Consider if its possible to extend it beyond mere regex into a c# function that takes a file path and returns a bool if it passes

## unix file permissions

Consider extended versions of unix file primissions, reasreach other tools that do something simliar; copy thier norms

## logging and failing

The deamon *MUST* not crash, the tools should be fail-safe and as simple as possible. The logging should be on the verbose side to the terminal

## instructions

Once the deamon is working as expected, there should be a .md document for other ai's to read that will get them up to speed for using this tool.

Produce a README.md when everything else is done

## languge

if today is `Tue Nov 25` write it in c# is the resreach document
