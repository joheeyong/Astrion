package com.astrion.gameserver.network;

import com.astrion.common.packet.GamePacket;
import com.astrion.common.packet.PacketType;
import io.netty.buffer.ByteBuf;
import io.netty.channel.ChannelHandlerContext;
import io.netty.handler.codec.ByteToMessageDecoder;

import java.nio.charset.StandardCharsets;
import java.util.List;

public class PacketDecoder extends ByteToMessageDecoder {

    @Override
    protected void decode(ChannelHandlerContext ctx, ByteBuf in, List<Object> out) {
        // Need at least 5 bytes: length(4) + packetType(1)
        if (in.readableBytes() < 4) {
            return;
        }

        in.markReaderIndex();
        int length = in.readInt();

        if (in.readableBytes() < length) {
            in.resetReaderIndex();
            return;
        }

        int packetCode = in.readByte() & 0xFF;
        byte[] payloadBytes = new byte[length - 1];
        in.readBytes(payloadBytes);

        String payload = new String(payloadBytes, StandardCharsets.UTF_8);
        PacketType type = PacketType.fromCode(packetCode);

        out.add(new GamePacket(type, payload));
    }
}
