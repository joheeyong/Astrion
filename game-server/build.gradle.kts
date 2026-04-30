plugins {
    application
}

application {
    mainClass.set("com.astrion.gameserver.GameServerMain")
}

dependencies {
    implementation(project(":common"))

    // Netty
    implementation("io.netty:netty-all:4.1.118.Final")

    // Redis (Lettuce - async Redis client)
    implementation("io.lettuce:lettuce-core:6.5.5.RELEASE")

    // Logging
    implementation("ch.qos.logback:logback-classic:1.5.16")
    implementation("org.slf4j:slf4j-api:2.0.16")

    // Jackson for JSON
    implementation("com.fasterxml.jackson.core:jackson-databind:2.18.3")
}

tasks.jar {
    manifest {
        attributes["Main-Class"] = "com.astrion.gameserver.GameServerMain"
    }
}
