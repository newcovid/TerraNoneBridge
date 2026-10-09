import asyncio
import websockets
import json
import sys
from datetime import datetime

# ==========================================
# TerraNoneBridge 调试服务器 (模拟 Nonebot 端)
# ==========================================

# 配置: 监听地址和端口 (需与模组配置一致)
HOST = "0.0.0.0" 
PORT = 7778

# 全局连接对象
current_websocket = None

def log(msg, type="信息"):
    """带时间戳的日志输出"""
    time_str = datetime.now().strftime("%H:%M:%S")
    print(f"[{time_str}] [{type}] {msg}")

async def handle_client(websocket):
    """处理 WebSocket 连接和消息接收"""
    global current_websocket
    current_websocket = websocket
    log(f"新连接建立，来自: {websocket.remote_address}", "网络")
    
    print_help() # 连接成功后提示可用指令

    try:
        async for message in websocket:
            try:
                data = json.loads(message)
                msg_type = data.get("type", "unknown")
                
                # 根据消息类型进行格式化输出
                if msg_type == "auth":
                    log(f"收到鉴权请求 | Token: {data.get('token')}", "鉴权")
                    
                    response = {
                        "type": "auth_response",
                        "success": True,
                        "message": "Debug Server Auto-Auth"
                    }
                    await websocket.send(json.dumps(response))
                    log("已发送自动鉴权通过响应", "鉴权")
                    
                elif msg_type == "log":
                    log(f"{data.get('msg')}", "模组日志")
                    
                elif msg_type == "chat":
                    log(f"<{data.get('nick')}> {data.get('msg')}", "聊天")
                    
                elif msg_type == "event":
                    payload = data.get('payload', '')
                    event_name = data.get('event_name', '事件')
                    log(f"[{event_name}] {payload}", "事件")
                    
                elif msg_type == "response":
                    log(f"{data.get('msg')}", "指令回执")
                    
                else:
                    log(f"原始数据包: {data}", "RAW")
                    
            except json.JSONDecodeError:
                log(f"收到非 JSON 消息: {message}", "警告")
            except Exception as e:
                log(f"消息处理错误: {e}", "错误")
                
    except websockets.exceptions.ConnectionClosed:
        log("客户端已断开连接", "网络")
    except Exception as e:
        log(f"连接异常: {e}", "错误")
    finally:
        current_websocket = None
        log("正在等待新的连接...", "状态")

async def send_packet(payload):
    """发送数据包到模组"""
    if not current_websocket:
        log("无客户端连接！无法发送消息。", "警告")
        return
    
    try:
        await current_websocket.send(json.dumps(payload))
        if payload.get('type') != 'cmd':
            log(f"已发送: {json.dumps(payload)}", "发送")
    except Exception as e:
        log(f"发送失败: {e}", "错误")

async def input_loop():
    """处理用户控制台输入"""
    print(f"\n服务器正在监听: ws://{HOST}:{PORT}")
    print("等待 Terraria 连接中...\n")

    while True:
        try:
            cmd_input = await asyncio.get_event_loop().run_in_executor(None, input)
        except EOFError:
            break
        
        if not cmd_input.strip():
            continue
            
        # --- 特殊调试指令 ---
        if cmd_input.startswith("!"):
            parts = cmd_input[1:].split()
            op = parts[0].lower()
            
            if op == "chat":
                if len(parts) < 3:
                    print("用法: !chat <昵称> <消息>")
                else:
                    await send_packet({
                        "type": "chat",
                        "nick": parts[1],
                        "msg": " ".join(parts[2:])
                    })
                    log(f"模拟群消息: [{parts[1]}] {' '.join(parts[2:])}", "测试")
            
            elif op == "json":
                try:
                    json_str = cmd_input[6:]
                    json_obj = json.loads(json_str)
                    await send_packet(json_obj)
                except Exception as e:
                    print(f"无效的 JSON 格式: {e}")

            elif op == "help":
                print_help()
            
            else:
                print("未知调试指令。输入 !help 查看列表。")
                
        # --- 普通模组指令 ---
        else:
            clean_cmd = cmd_input.strip()
            if clean_cmd.startswith("/tnb "):
                clean_cmd = clean_cmd[5:]
            elif clean_cmd.startswith("/"):
                clean_cmd = clean_cmd[1:]

            # [修复] 拆分指令和参数
            # 这里的 split() 不带参数会以任意空白字符分割，适合处理 inv new 或 inv  new
            parts = clean_cmd.split()
            
            if not parts:
                continue

            cmd_name = parts[0]
            cmd_args = parts[1:] if len(parts) > 1 else []
                
            await send_packet({
                "type": "command",
                "command": cmd_name,
                "args": cmd_args
            })
            log(f"发送指令: {cmd_name} 参数: {cmd_args}", "指令输出")

def print_help():
    print("\n" + "="*50)
    print(" [TerraNoneBridge 调试控制台]")
    print("="*50)
    print("1. 直接指令测试:")
    print("   输入任意指令以在游戏中执行。")
    print("   示例:")
    print("     help           -> /tnb help")
    print("     inv PlayerName -> /tnb inv PlayerName")
    print("")
    print("2. 模拟工具:")
    print("   !chat <昵称> <消息>  : 模拟发送 QQ 群消息到游戏")
    print("   !json <JSON字符串>   : 发送原始 JSON 数据包")
    print("   !help               : 显示此菜单")
    print("="*50 + "\n")

async def main():
    server = await websockets.serve(handle_client, HOST, PORT)
    input_task = asyncio.create_task(input_loop())
    await asyncio.Future() 

if __name__ == "__main__":
    try:
        if sys.platform == 'win32':
            asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\n服务器已停止。")