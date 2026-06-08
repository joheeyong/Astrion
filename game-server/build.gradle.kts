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

    // Tests — JUnit 5. First test landing here is PlayerStateLocksTest,
    // covering the race-condition audit landing alongside it. Future
    // regression tests for TradeManager / AuctionManager (without a live
    // Redis dependency they need a mock; for now we exercise the lock
    // primitive directly).
    testImplementation("org.junit.jupiter:junit-jupiter:5.10.2")
    testRuntimeOnly("org.junit.platform:junit-platform-launcher")
}

tasks.test {
    useJUnitPlatform()
}

tasks.jar {
    manifest {
        attributes["Main-Class"] = "com.astrion.gameserver.GameServerMain"
    }
}
