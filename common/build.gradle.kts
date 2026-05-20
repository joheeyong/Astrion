dependencies {
    implementation("com.google.code.gson:gson:2.11.0")
}

// Cross-language version sync gate. The wire-compatible game version lives in
// three places (root build.gradle.kts, Version.java, Version.cs); this task
// fails the build the moment they drift. Run ./bump-version.sh <X.Y.Z> to bump
// all three together.
tasks.register("checkVersionSync") {
    group = "verification"
    description = "Verify root version, Version.java and Version.cs are aligned."
    doLast {
        val rootVer = project.rootProject.version.toString()
        val javaFile = rootProject.file("common/src/main/java/com/astrion/common/Version.java")
        val csFile = rootProject.file("unity-client/Assets/Scripts/Network/Version.cs")

        val javaVer = Regex("""CURRENT\s*=\s*"([^"]+)"""")
            .find(javaFile.readText())?.groupValues?.get(1)
            ?: throw GradleException("Could not parse CURRENT from ${javaFile}")

        val csVer = Regex("""Current\s*=\s*"([^"]+)"""")
            .find(csFile.readText())?.groupValues?.get(1)
            ?: throw GradleException("Could not parse Current from ${csFile}")

        if (rootVer != javaVer || javaVer != csVer) {
            throw GradleException(
                "Version desync: root=$rootVer, Version.java=$javaVer, Version.cs=$csVer. " +
                "Run ./bump-version.sh <X.Y.Z> from the repo root.")
        }
        println("Version sync OK: $rootVer")
    }
}

// Block every Java compile in common (and transitively any module that
// depends on common) until the three files agree.
tasks.named("compileJava") { dependsOn("checkVersionSync") }
