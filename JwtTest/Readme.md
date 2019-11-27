**摘要：**

本文演示如何向有效用户提供jwt，以及如何在webapi中使用该token通过JwtBearerMiddleware中间件对用户进行身份认证。

**认证和授权的区别？**

首先我们要弄清楚认证（Authentication）和授权（Authorization）的区别，以免混淆了。认证是确认的过程中你是谁，而授权围绕是你被允许做什么，即权限。显然，在确认允许用户做什么之前，你需要知道他们是谁，因此，在需要授权时，还必须以某种方式对用户进行身份验证。 

**什么是JWT？**

根据维基百科的定义，JSON WEB Token（JWT, 读作 [/dʒɒt/]），是一种基于JSON的、用于在网络上声明某种主张的令牌（token）。JWT通常由三部分组成：头信息（header），消息体（payload）和签名（signature）。

头信息指定了该JWT使用的签名算法:

```
header = '{"alg":"HS256","typ":"JWT"}'
```

`HS256`表示使用了HMAC-SHA256来生成签名。

消息体包含了JWT的意图：

```
payload = '{"loggedInAs":"admin","iat":1422779638}'//iat表示令牌生成的时间
```

未签名的令牌由`base64url`编码的头信息和消息体拼接而成（使用"."分隔），签名则通过私有的key计算而成：

```
key = 'secretkey'  unsignedToken = encodeBase64(header) + '.' + encodeBase64(payload)  signature = HMAC-SHA256(key, unsignedToken)
```

最后在未签名的令牌尾部拼接上`base64url`编码的签名（同样使用"."分隔）就是JWT了：

```
token = encodeBase64(header) + '.' + encodeBase64(payload) + '.' + encodeBase64(signature)# token看起来像这样: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJsb2dnZWRJbkFzIjoiYWRtaW4iLCJpYXQiOjE0MjI3Nzk2Mzh9.gzSraSYS8EXBxLN_oWnFSRgCzcmJmMjLiuyu5CSpyHI
```

JWT常常被用作保护服务端的资源（resource），客户端通常将JWT通过HTTP的`Authorization` header发送给服务端，服务端使用自己保存的key计算、验证签名以判断该JWT是否可信：

```
Authorization: Bearer eyJhbGci*...<snip>...*yu5CSpyHI
```

**准备工作**

使用vs2019创建webapi项目，并且安装nuget包

- 

```
Microsoft.AspNetCore.Authentication.JwtBearer
```

![img](https://mmbiz.qpic.cn/mmbiz_png/L3GzzkxmBjNBWUCsKeVeH6w3AE05DVVaA3REticN7QZd33gHvV1BxJDfyv1zYx59x2W3wgViclrH6ticJib7cdGTicw/640?wx_fmt=png&tp=webp&wxfrom=5&wx_lazy=1&wx_co=1)

##### **Startup类**

- ConfigureServices 添加认证服务

```
services.AddAuthentication(options =>            {                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;            }).AddJwtBearer(options =>            {                options.SaveToken = true;                options.RequireHttpsMetadata = false;                options.TokenValidationParameters = new TokenValidationParameters()                {                    ValidateIssuer = true,                    ValidateAudience = true,                    ValidAudience = "https://www.cnblogs.com/chengtian",                    ValidIssuer = "https://www.cnblogs.com/chengtian",                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SecureKeySecureKeySecureKeySecureKeySecureKeySecureKey"))                };            });
```

- Configure 配置认证中间件

```
  app.UseAuthentication();//认证中间件
```

**创建一个token**

- 添加一个登录model命名为LoginInput

```
public class LoginInput    {        public string Username { get; set; }        public string Password { get; set; }    }
```

- 添加一个认证控制器命名为AuthenticateController

```
  [Route("api/[controller]")]    public class AuthenticateController : Controller    {        [HttpPost]        [Route("login")]        public IActionResult Login([FromBody]LoginInput input)        {            //从数据库验证用户名，密码             //验证通过 否则 返回Unauthorized            //创建claim            var authClaims = new[] {                new Claim(JwtRegisteredClaimNames.Sub,input.Username),                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())            };            IdentityModelEventSource.ShowPII = true;            //签名秘钥 可以放到json文件中            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SecureKeySecureKeySecureKeySecureKeySecureKeySecureKey"));            var token = new JwtSecurityToken(                   issuer: "https://www.cnblogs.com/chengtian",                   audience: "https://www.cnblogs.com/chengtian",                   expires: DateTime.Now.AddHours(2),                   claims: authClaims,                   signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)                   );            //返回token和过期时间            return Ok(new            {                token = new JwtSecurityTokenHandler().WriteToken(token),                expiration = token.ValidTo            });        }    }
```

##### **添加api资源**

**利用默认的控制器****WeatherForecastController**

- 添加个Authorize标签
- 路由调整为：[Route("api/[controller]")] 代码如下

```
 [Authorize]    [ApiController]    [Route("api/[controller]")]    public class WeatherForecastController : ControllerBase
```

到此所有的代码都已经准好了，下面进行运行测试

#### 运行项目

使用postman进行模拟

- 输入url:https://localhost:44364/api/weatherforecast

- ![img](https://mmbiz.qpic.cn/mmbiz_png/L3GzzkxmBjNBWUCsKeVeH6w3AE05DVVaZB9y2AEyTrPUwicHLHv8mG6h1j7rVr7g6r32tNptibeGlTOjO7ndUTMA/640?wx_fmt=png&tp=webp&wxfrom=5&wx_lazy=1&wx_co=1)

  发现返回时401未认证，下面获取token

- 通过用户和密码获取token

  ![img](https://mmbiz.qpic.cn/mmbiz_png/L3GzzkxmBjNBWUCsKeVeH6w3AE05DVVaXfaPZQIR3iaRkicuvBqOSHn2QVHStnWxZEGtELtoSp2icPycB9d3X4oyw/640?wx_fmt=png&tp=webp&wxfrom=5&wx_lazy=1&wx_co=1)

  如果我们的凭证正确，将会返回一个token和过期日期，然后利用该令牌进行访问

- 利用token进行请求

  ![img](https://mmbiz.qpic.cn/mmbiz_png/L3GzzkxmBjNBWUCsKeVeH6w3AE05DVVaL1zMk3HicWuXDPVgHIic95Y9pzVZGEK7qNjTBhd16Mw7KVHwTWxzrAPQ/640?wx_fmt=png&tp=webp&wxfrom=5&wx_lazy=1&wx_co=1)

  ok，最后发现请求状态200！



原文链接: https://mp.weixin.qq.com/s/WMTBOF4Efu4RYdFaf4k29w 

