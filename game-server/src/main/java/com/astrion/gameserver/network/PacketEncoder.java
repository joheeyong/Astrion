package com.astrion.gameserver.network;

import com.astrion.common.packet.GamePacket;
import io.netty.buffer.ByteBuf;
import io.netty.channel.ChannelHandlerContext;
import io.netty.handler.codec.MessageToByteEncoder;

import java.nio.charset.StandardCharsets;

public class PacketEncoder extends MessageToByteEncoder<GamePacket> {

    @Override
    protected void encode(ChannelHandlerContext ctx, GamePacket msg, ByteBuf out) {
        byte[] payloadBytes = msg.getPayload().getBytes(StandardCharsets.UTF_8);
        int length = 1 + payloadBytes.length; // packetType(1) + payload

        out.writeInt(length);
        out.writeByte(msg.getType().getCode());
        out.writeBytes(payloadBytes);
    }
}
