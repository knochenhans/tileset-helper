#!/usr/bin/env python

import os

# Mirror the other extensions in this workspace: delegate to the sibling
# godot-cpp build, then compile our editor plugin sources into bin/.
env = SConscript("../godot-cpp/SConstruct")

env.Append(CPPPATH=["src/"])
sources = Glob("src/*.cpp")

if env["platform"] == "macos":
    library = env.SharedLibrary(
        "bin/libtileset_helper.{}.{}.framework/libtileset_helper.{}.{}".format(
            env["platform"], env["target"], env["platform"], env["target"]
        ),
        source=sources,
    )
else:
    library = env.SharedLibrary(
        "bin/libtileset_helper{}{}".format(env["suffix"], env["SHLIBSUFFIX"]),
        source=sources,
    )

env.NoCache(library)

Default(library)
