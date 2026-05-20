package com.astrion.common;

public final class Version {
    private Version() {}

    /** Single source of truth for the wire-compatible game version. Bump on any
     *  change that breaks compatibility (packet format, mandatory new fields, etc.). */
    public static final String CURRENT = "0.1.0";
}
