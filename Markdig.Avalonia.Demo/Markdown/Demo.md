# 原文链接

[Naccl's Blog](https://naccl.top/blog/24)

# 正文

笔记来自[B站视频](https://www.bilibili.com/video/BV1cb4y1o7zz)及个人实践，主要用于复习，不知对于初学是否有帮助

所有代码可以在这里找到[https://github.com/Naccl/RabbitMQ-Hello](https://github.com/Naccl/RabbitMQ-Hello)

# 消息队列

## MQ的相关概念

### 什么是MQ

MQ(message queue)，从字面意思上看，本质是个队列，FIFO 先入先出，只不过队列中存放的内容是 message 而已，还是一种跨进程的通信机制，用于上下游传递消息。在互联网架构中，MQ 是一种非常常见的上下游“逻辑解耦+物理解耦”的消息通信服务。使用了 MQ 之后，消息发送上游只需要依赖 MQ，不用依赖其他服务。

### 为什么要用MQ

1. **流量削峰**

   举个例子，如果订单系统最多能处理一万次订单，这个处理能力应付正常时段的下单时绰绰有余，正常时段我们下单一秒后就能返回结果。但是在高峰期，如果有两万次下单操作系统是处理不了的，只能限制订单超过一万后不允许用户下单。使用消息队列做缓冲，我们可以取消这个限制，把一秒内下的订单分散成一段时间来处理，这时有些用户可能在下单十几秒后才能收到下单成功的操作，但是比不能下单的体验要好。

2. **应用解耦**

   以电商应用为例，应用中有订单系统、库存系统、物流系统、支付系统。用户创建订单后，如果耦合调用库存系统、物流系统、支付系统，任何一个子系统出了故障，都会造成下单操作异常。当转变成基于消息队列的方式后，系统间调用的问题会减少很多，比如物流系统因为发生故障，需要几分钟来修复。在这几分钟的时间里，物流系统要处理的内存被缓存在消息队列中，用户的下单操作可以正常完成。当物流系统恢复后，继续处理订单信息即可，中单用户感受不到物流系统的故障，提升系统的可用性。

   ![image-20211021150003797](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211021150003797.png)

3. **异步处理**

   有些服务间调用是异步的，例如 A 调用 B，B 需要花费很长时间执行，但是 A 需要知道 B 什么时候可以执行完，以前一般有两种方式，A 过一段时间去调用 B的查询 api。或者 A 提供一个callback api，B 执行完之后调用 api 通知 A 服务。这两种方式都不是很优雅，使用消息总线，可以很方便解决这个问题，A 调用 B 服务后，只需要监听 B 处理完成的消息，当 B 处理完成后，会发送一条消息给 MQ，MQ 会将此消息转发给 A 服务。这样 A 服务既不用循环调用 B 的查询 api，也不用提供 callback api。同样 B 服务也不用做这些操作。A 服务还能及时地得到异步处理成功的消息。

### MQ的选择

1. Kafka
   Kafka 主要特点是基于 Pull 的模式来处理消息消费，追求高吞吐量，一开始的目的就是用于日志收集和传输，适合产生**大量数据**的互联网服务的数据收集业务。**大型公司**建议可以选用，如果有**日志采集**功能，肯定是首选 kafka 了。
2. RocketMQ
   天生为金融互联网领域而生，对于可靠性要求很高的场景，尤其是电商里面的订单扣款，以及业务削峰，在大量交易涌入时，后端可能无法及时处理的情况。RocketMQ 在稳定性上可能更值得信赖，这些业务场景在阿里双11已经经历了多次考验，如果你的业务有上述并发场景，建议可以选择 RocketMQ。
3. RabbitMQ
   结合 erlang 语言本身的并发优势，性能好**时效性微秒级**，**社区活跃度也比较高**，管理界面用起来十分方便，如果你的**数据量没有那么大**，中小型公司优先选择功能比较完备的 RabbitMQ。

## RabbitMQ

### 概念

RabbitMQ 是一个消息中间件：它接受并转发消息。你可以把它当做一个快递站点，当你要发送一个包裹时，你把你的包裹放到快递站，快递员最终会把你的快递送到收件人那里，按照这种逻辑 RabbitMQ 是一个快递站，一个快递员帮你传递快件。RabbitMQ 与快递站的主要区别在于，它不处理快件而是接收，存储和转发消息数据。

### 四大核心概念

1. **生产者**：产生数据发送消息的程序是生产者
2. **交换机**：交换机是 RabbitMQ 非常重要的一个部件，一方面它接收来自生产者的消息，另一方面它将消息推送到队列中。交换机必须确切知道如何处理它接收到的消息，是将这些消息推送到特定队列还是推送到多个队列，亦或者是把消息丢弃，这个得由交换机类型决定
3. **队列**：队列是 RabbitMQ 内部使用的一种数据结构，尽管消息流经 RabbitMQ 和应用程序，但它们只能存储在队列中。队列仅受主机的内存和磁盘限制的约束，本质上是一个大的消息缓冲区。许多生产者可以将消息发送到一个队列，许多消费者可以尝试从一个队列接收数据，这就是我们使用队列的方式
4. **消费者**：消费与接收具有相似的含义。消费者大多时候是一个等待接收消息的程序。请注意生产者，消费者和消息中间件很多时候并不在同一机器上。同一个应用程序既可以是生产者又是可以是消费者。

### 各个名词介绍

![image-20211021151657852](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211021151657852.png)

1. **Broker**：接收和分发消息的应用，RabbitMQ Server 就是 Message Broker
2. **Virtual host**：出于多租户和安全因素设计的，把 AMQP 的基本组件划分到一个虚拟的分组中，类似于网络中的 namespace 概念。当多个不同的用户使用同一个 RabbitMQ server 提供的服务时，可以划分出多个 vhost，每个用户在自己的 vhost 创建 exchange / queue 等
3. **Connection**：publisher / consumer 和 broker 之间的 TCP 连接
4. **Channel**：如果每一次访问 RabbitMQ 都建立一个 Connection，在消息量大的时候建立 TCP Connection 的开销将是巨大的，效率也较低。Channel 是在connection 内部建立的逻辑连接，如果应用程序支持多线程，通常每个 thread 创建单独的 channel 进行通讯，AMQP method 包含了 channel id 帮助客户端和 message broker 识别 channel，所以 channel 之间是完全隔离的。Channel 作为轻量级的 Connection 极大减少了操作系统建立 TCP connection 的开销
5. **Exchange**：message 到达 broker 的第一站，根据分发规则，匹配查询表中的 routing key，分发消息到 queue 中去。常用的类型有：direct (point-to-point), topic (publish-subscribe) and fanout (multicast)
6. **Queue**：消息最终被送到这里等待 consumer 取走
7. **Binding**：exchange 和 queue 之间的虚拟连接，binding 中可以包含 routing key，Binding 信息被保存到 exchange 中的查询表中，用于 message 的分发依据

### 安装

记录一下 Ubuntu 18.04 的安装最新版 Erlang 24.0 和 RabbitMQ 3.9.8 的过程

Ps. 一开始图方便直接 apt-get 安装了旧版的 Erlang 和 RabbitMQ（Ubuntu 的 apt 仓库太旧），到了延迟队列插件环节，发现插件竟然装不上。网上一搜安装新版的方法，清一色 apt-get

#### 安装Erlang 24.0

[https://www.erlang-solutions.com/downloads/](https://www.erlang-solutions.com/downloads/)

```shell
# To add Erlang Solutions repository (including our public key for apt-secure) to your system, call the following commands:
wget https://packages.erlang-solutions.com/erlang-solutions_2.0_all.deb
sudo dpkg -i erlang-solutions_2.0_all.deb

# Next, add the Erlang Solutions public key for "apt-secure" using following commands:
wget https://packages.erlang-solutions.com/ubuntu/erlang_solutions.asc
sudo apt-key add erlang_solutions.asc

# Refresh the repository cache and install either the "erlang" package:
sudo apt-get update
sudo apt-get install erlang
```

#### 安装RabbitMQ 3.9.8

[https://rabbitmq.com/install-debian.html](https://rabbitmq.com/install-debian.html)

使用 dkpg 手动安装

```shell
# sync package metadata
sudo apt-get update
# install dependencies manually
sudo apt-get -y install socat logrotate init-system-helpers adduser

# download the package
sudo apt-get -y install wget
wget https://github.com/rabbitmq/rabbitmq-server/releases/download/v3.9.8/rabbitmq-server_3.9.8-1_all.deb

# install the package with dpkg
sudo dpkg -i rabbitmq-server_3.9.8-1_all.deb
```

访问 GitHub 链接时由于众所周知的原因，如果速度太慢，可以选择本地下载，再传到 Ubuntu

### 常用命令

```shell
# 开机自启动
systemctl enable rabbitmq-server
# 启动服务
systemctl start rabbitmq-server
# 关闭服务
systemctl stop rabbitmq-server
# 查看状态
systemctl status rabbitmq-server
```

### 开启Web界面插件

```shell
# 启用插件
rabbitmq-plugins enable rabbitmq_management
# 添加用户
rabbitmqctl add_user username password
# 设置用户角色
rabbitmqctl set_user_tags username administrator
# 设置用户权限
rabbitmqctl set_permissions -p "/" username ".*" ".*" ".*"
```

如果使用的是云服务器，需要开启防火墙组策略中“5672”和“15672”端口

# 轮询分发消息

## 消费者

```java
public class Worker01 {
	public static final String QUEUE_NAME = "hello";

	public static void main(String[] args) throws IOException {
		//获取信道
		Channel channel = RabbitMqUtils.getChannel();
		//消息接收
		DeliverCallback deliverCallback = (consumerTag, message) -> {
			System.out.println("接收到的消息：" + new String(message.getBody()));
		};
		//消息接收被取消时，执行下面的内容
		CancelCallback cancelCallback = consumerTag -> {
			System.out.println("消费者取消消费接口回调逻辑");
		};
		/**
		 * 消费者消费消息
		 * @param queue 消费哪个队列
		 * @param autoAck 自动应答为true，手动应答为false
		 * @param deliverCallback 消费消息时回调
		 * @param cancelCallback 消费者取消消费时回调
		 */
		channel.basicConsume(QUEUE_NAME, true, deliverCallback, cancelCallback);
	}
}
```

## 生产者

```java
public class Test01 {
	//队列名称
	public static final String QUEUE_NAME = "hello";

	public static void main(String[] args) throws IOException {
		//获取信道
		Channel channel = RabbitMqUtils.getChannel();
		/**
		 * 声明队列
		 * @param queue 队列名称
		 * @param durable 队列中的消息是否持久化，默认情况消息存储在内存中
		 * @param exclusive 是否排他队列
		 * @param autoDelete 是否自动删除队列，最后一个消费者断开连接后自动删除队列
		 * @param arguments 其它参数
		 */
		channel.queueDeclare(QUEUE_NAME, false, false, false, null);
		//从控制台接收参数
		Scanner scanner = new Scanner(System.in);
		while (scanner.hasNext()) {
			String msg = scanner.next();
			/**
			 * 发送消息
			 * @param exchange 发送到哪个交换机
			 * @param routingKey 路由的key，队列名称
			 * @param props 其它参数
			 * @param body 消息体
			 */
			channel.basicPublish("", QUEUE_NAME, null, msg.getBytes());
			System.out.println("发送消息完成：" + msg);
		}
	}
}
```

## 执行结果

启动一个生产者，发送大量消息。

启动多个消费者，将会轮流接收消息，且每个消息只会被消费一次。

# 消息应答

## 概念

消费者完成一个任务可能需要一段时间，如果其中一个消费者处理一个耗时较长的任务并仅只完成了部分突然它挂掉了，会发生什么情况。RabbitMQ 一旦向消费者传递了一条消息，便立即将该消息标记为删除。在这种情况下，突然有个消费者挂掉了，我们将丢失正在处理的消息，以及后续发送给该消费者的消息，因为它无法接收到。

为了保证消息在发送过程中不丢失，RabbitMQ 引入消息应答机制，消息应答就是：**消费者在接收到消息并且处理该消息之后，告诉 RabbitMQ 它已经处理了，RabbitMQ 可以把该消息删除了。**

## 自动应答

消息发送后立即被认为已经传送成功，这种模式需要**在高吞吐量和数据传输安全性方面做权衡**，因为这种模式如果消息在接收到之前，消费者那边出现连接或者 channel 关闭，那么消息就丢失了，当然另一方面这种模式消费者那边可以传递过载的消息，没有对传递的消息数量进行限制，当然这样有可能使得消费者这边由于接收太多还来不及处理的消息，导致这些消息的积压，最终使得内存耗尽，最终这些消费者线程被操作系统杀死，所以这种模式仅适用在消费者可以高效并以某种速率能够处理这些消息的情况下使用。

## 消息应答的方法

- channel.basicAck()，用于肯定确认，RabbitMQ 已经知道该消息成功被处理，可以将其丢弃了
- channel.basicNack()，用于否定确认
- channel.basicReject()，用于否定确认

## Multiple的解释

手动应答的好处是可以批量应答并且减少网络拥堵

```java
channel.basicAck(deliveryTag, true);//true表示批量应答
```

true 代表批量应答 channel 上未应答的消息，比如当前 channel 上有 tag 为 5,6,7,8 的消息，当前 tag 是8，那么此时 5-8 这些还未应答的消息都会被确认收到消息应答

false 只会应答 tag=8 的消息，5,6,7 这三个消息依然不会被确认收到消息应答

## 消息自动重新入队

如果消费者由于某些原因失去连接（其通道已关闭，连接已关闭或 TCP 连接丢失），导致消息未发送 ACK 确认，RabbitMQ 将了解到消息未完全处理，并将对其重新排队。如果此时其他消费者可以处理，它将很快将其重新分发给另一个消费者。这样，即使某个消费者偶尔死亡，也可以确保不会丢失任何消息。

![image-20211014184912712](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211014184912712.png)

## 消息手动应答代码

消费者设置手动应答

```java
//设置手动应答
boolean autoAck = false;
channel.basicConsume(QUEUE_NAME, autoAck, deliverCallback, cancelCallback);
```

手动应答

```java
DeliverCallback deliverCallback = (consumerTag, message) -> {
	System.out.println("接收到消息:" + new String(message.getBody()));
	//手动应答
	channel.basicAck(message.getEnvelope().getDeliveryTag(), false);//false不批量应答
};
```

## 手动应答效果

1. 启动第一个消费者开启手动应答，每次接收到消息后 sleep 30秒，`Thread.sleep(30000);`
2. 启动第二个消费者开启手动应答，每次接收到消息后 sleep 1秒，`Thread.sleep(1000);`
3. 启动一个生产者快速发送多个消息
4. 此时消费者1接收到第一个消息并进入 sleep，消费者2每秒接收一个消息
5. 结束消费者1的进程，消费者1尚未处理完的消息将重新入队，并排在队头，并被消费者2重新消费

# 不公平分发

在最开始的时候我们学习到 RabbitMQ 分发消息采用的轮训分发，但是在某种场景下这种策略并不是很好，比方说有两个消费者在处理任务，其中有个消费者1处理任务的速度非常快，而另外一个消费者2处理速度却很慢，这个时候我们还是采用轮训分发的话就会到这处理速度快的这个消费者很大一部分时间处于空闲状态，而处理慢的那个消费者一直在干活，这种分配方式在这种情况下其实就不太好，但是 RabbitMQ 并不知道这种情况，它依然很公平的进行分发。

为了避免这种情况，可以在**消费者代码中**设置参数`channel.basicQos(1);`

```java
int prefetchCount = 1;
channel.basicQos(prefetchCount);
```

![image-20211014164646480](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211014164646480.png)

意思就是如果这个任务我还没有处理完或者我还没有应答你，你先别分配给我，我目前只能处理一个任务，然后 RabbitMQ 就会把该任务分配给没有那么忙的那个空闲消费者，当然如果所有的消费者都没有完成手上任务，队列还在不停的添加新任务，队列有可能就会遇到队列被撑满的情况，这个时候就只能添加新的 worker 或者改变其他存储任务的策略。

## 预取值

简单来说就是限制**通道上允许接收未确认消息的最大数量**

比如：

消费者1

```java
int prefetchCount = 2;
channel.basicQos(prefetchCount);
```

消费者2

```java
int prefetchCount = 5;
channel.basicQos(prefetchCount);
```

那么 MQ 给消费者1传递2条消息，给消费者2传递5条消息后，就会停止传递。直到消费者确认消息后，才会再次在通道上传递消息。

# 持久化

## 概念

默认情况下 RabbitMQ 退出或由于某种原因崩溃时，它忽视队列和消息（队列和消息都将丢失）。确保消息不会丢失需要做两件事：**将队列和消息都标记为持久化**。

## 队列如何实现持久化

在声明队列时把 `durable`参数设置为持久化

```java
boolean durable = true;
channel.queueDeclare(QUEUE_NAME, durable, false, false, null);
```

如果之前声明的队列不是持久化的，需要把原先队列先删除，或者重新创建一个持久化的队列，否则就会报错

![image-20211014162655424](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211014162655424.png)

以下为控制台中持久化与非持久化队列的 UI 显示区：

![image-20211014162607178](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211014162607178.png)

这个时候即使重启 RabbitMQ 队列也依然存在

## 消息如何实现持久化

要想让消息实现持久化需要在消息生产者修改代码，`MessageProperties.PERSISTENT_TEXT_PLAIN`添加这个属性。

```java
channel.basicPublish("", QUEUE_NAME, MessageProperties.PERSISTENT_TEXT_PLAIN, msg.getBytes());
```

将消息标记为持久化并不能完全保证不会丢失消息。尽管它告诉 RabbitMQ 将消息保存到磁盘，但是这里依然存在当消息刚准备存储在磁盘的时候但是还没有存储完，消息还在缓存的一个间隔点。此时并没有真正写入磁盘。持久性保证并不强，但是对于我们的简单任务队列而言，这已经绰绰有余了。如果需要更强有力的持久化策略，需要进行发布确认。

# 发布确认

## 发布确认原理

生产者将信道设置成 confirm 模式，一旦信道进入 confirm 模式，所有在该信道上面发布的消息都将会被指派一个唯一的 ID (从1开始)，一旦消息被投递到所有匹配的队列之后，broker 就会发送一个确认给生产者(包含消息的唯一 ID)，这就使得生产者知道消息已经正确到达目的队列了，如果消息和队列是可持久化的，那么确认消息会在将消息写入磁盘之后发出，broker 回传给生产者的确认消息中 deliveryTag 域包含了确认消息的序列号，此外 broker 也可以设置 basic.ack 的 multiple 域，表示到这个序列号之前的所有消息都已经得到了处理。

confirm 模式最大的好处在于他是异步的，一旦发布一条消息，生产者应用程序就可以在等信道返回确认的同时继续发送下一条消息，当消息最终得到确认之后，生产者应用便可以通过回调方法来处理该确认消息，如果 RabbitMQ 因为自身内部错误导致消息丢失，就会发送一条 nack 消息，生产者应用程序同样可以在回调方法中处理该 nack 消息。

**通过发布确认，在消息持久化中，保存到磁盘的间隔点 RabbitMQ 可能发生宕机的问题就能够解决。**

## 发布确认的策略

### 开启发布确认

发布确认默认是没有开启的，如果要开启需要调用方法 confirmSelect，每当你要想使用发布确认，都需要在 channel 上调用该方法

```java
//开启发布确认
channel.confirmSelect();
```

### 单个确认发布

这是一种简单的确认方式，它是一种同步确认发布的方式，也就是发布一个消息之后只有它被确认发布，后续的消息才能继续发布，`bool waitForConfirms(long timeout)`这个方法返回消息是否在指定时间范围内被确认。

这种确认方式有一个最大的缺点就是：发布速度特别慢，因为如果没有确认发布的消息就会阻塞所有后续消息的发布，这种方式最多提供每秒不超过数百条发布消息的吞吐量。

```java
public static void publishMessageSignal() throws IOException, InterruptedException {
	Channel channel = RabbitMqUtils.getChannel();
	String queueName = UUID.randomUUID().toString();
	channel.queueDeclare(queueName, true, false, false, null);
	//开启发布确认
	channel.confirmSelect();
	long begin = System.currentTimeMillis();
	for (int i = 0; i < MESSAGE_COUNT; i++) {
		String msg = i + "";
		channel.basicPublish("", queueName, MessageProperties.PERSISTENT_TEXT_PLAIN, msg.getBytes());
		//单个消息就马上进行发布确认
		boolean flag = channel.waitForConfirms();
		if (flag) {
			System.out.println("消息发送成功" + i);
		}
	}
	long end = System.currentTimeMillis();
	System.out.println("发布" + MESSAGE_COUNT + "个单独确认消息，耗时" + (end - begin) + "ms");
}
```

### 批量发布确认

上面那种方式非常慢，与单个等待确认消息相比，先发布一批消息然后一起确认可以极大地提高吞吐量，当然这种方式的缺点就是：当发生故障导致发布出现问题时，不知道是哪个消息出现问题了，我们必须将整个批处理保存在内存中，以记录重要的信息而后重新发布消息。当然这种方案仍然是同步的，也一样阻塞消息的发布。

```java
public static void publishMessageBatch() throws IOException, InterruptedException {
	Channel channel = RabbitMqUtils.getChannel();
	String queueName = UUID.randomUUID().toString();
	channel.queueDeclare(queueName, true, false, false, null);
	//开启发布确认
	channel.confirmSelect();
	long begin = System.currentTimeMillis();
	//批量确认阈值
	int batchSize = 100;
	for (int i = 0; i < MESSAGE_COUNT; i++) {
		String msg = i + "";
		channel.basicPublish("", queueName, MessageProperties.PERSISTENT_TEXT_PLAIN, msg.getBytes());
		//批量确认消息
		if ((i + 1) % batchSize == 0) {
			channel.waitForConfirms();
			System.out.println("消息发送成功" + i);
		}
	}
	long end = System.currentTimeMillis();
	System.out.println("发布" + MESSAGE_COUNT + "个批量确认消息，耗时" + (end - begin) + "ms");
}
```

### 异步确认发布

异步确认发布通过函数回调来保证是否投递成功，一旦消息丢失，可以马上知道哪些消息丢失了。

把未确认的消息放到一个基于内存的能被发布线程访问的队列，比如说用 ConcurrentSkipListMap 这个 Map 在 confirm callbacks 与发布线程之间进行消息的传递，最终就能得到所有未确认的消息。

```java
public static void publishMessageAsync() throws IOException {
	Channel channel = RabbitMqUtils.getChannel();
	String queueName = UUID.randomUUID().toString();
	channel.queueDeclare(queueName, true, false, false, null);
	//开启发布确认
	channel.confirmSelect();
	/**
	 * 线程安全有序的一个hash表 适用于高并发的情况下
	 * 1.轻松地将序号与消息进行关联
	 * 2.轻松批量删除条目 只要给到序号
	 * 3.支持高并发（多线程）
	 */
	ConcurrentSkipListMap<Long, String> outstandingConfirms = new ConcurrentSkipListMap<>();
	//消息确认成功 回调函数
	ConfirmCallback ackCallback = (deliveryTag, multiple) -> {
		//2.删除掉已经确认的消息 剩下的就是未确认的消息
		if (multiple) {
			ConcurrentNavigableMap<Long, String> confirmed = outstandingConfirms.headMap(deliveryTag);
			confirmed.clear();
		} else {
			outstandingConfirms.remove(deliveryTag);
		}
		System.out.println("确认的消息：" + deliveryTag);
	};
	//消息确认失败 回调函数
	ConfirmCallback nackCallback = (deliveryTag, multiple) -> {
		//3.打印一下未确认的消息都有哪些
		String msg = outstandingConfirms.get(deliveryTag);
		System.out.println("未确认的消息：" + msg + ", tag:" + deliveryTag);
	};
	channel.addConfirmListener(ackCallback, nackCallback);
	long begin = System.currentTimeMillis();
	//批量发送消息
	for (int i = 0; i < MESSAGE_COUNT; i++) {
		String msg = i + "";
		//1.此处记录下所有要发送的消息
		outstandingConfirms.put(channel.getNextPublishSeqNo(), msg);
		channel.basicPublish("", queueName, MessageProperties.PERSISTENT_TEXT_PLAIN, msg.getBytes());
		System.out.println("消息发送成功" + i);
	}
	long end = System.currentTimeMillis();
	System.out.println("发布" + MESSAGE_COUNT + "个异步确认消息，耗时" + (end - begin) + "ms");
}
```

### 三种发布确认方式对比

- 单个确认发布：同步等待确认，简单，但吞吐量非常有限。
- 批量确认发布：批量同步等待确认，简单，合理的吞吐量，一旦出现问题但很难推断出是那条消息出现了问题。
- 异步确认发布：最佳性能和资源使用，在出现错误的情况下可以很好地控制。

**用异步确认发布就完事了。**

# 交换机

在之前我们创建的是工作队列，我们假设的是工作队列背后，每个任务都恰好交付给一个消费者(工作进程)。在这一部分中，我们将做一些完全不同的事情：我们将消息传达给多个消费者。这种模式称为“发布/订阅”。

为了说明这种模式，我们将构建一个简单的日志系统。它将由两个程序组成：第一个程序将发出日志消息，第二个程序是消费者。其中我们会启动两个消费者，事实上第一个程序发出的日志消息将广播给所有消费者。

## Exchanges

### 概念

RabbitMQ 消息传递模型的核心思想是：**生产者生产的消息从不会直接发送到队列**。实际上，通常生产者甚至都不知道这些消息传递到了哪些队列中。

相反，**生产者只能将消息发送到交换机(exchange)**，交换机工作的内容非常简单，一方面它接收来自生产者的消息，另一方面将它们推入队列。交换机必须确切知道如何处理收到的消息。是应该把这些消息放到特定队列还是说把他们放到许多队列中还是说应该丢弃它们。这就由交换机的类型来决定。

### 类型

总共有以下类型：

- 直接（direct）
- 主题（topic）
- 标题（headers）
- 扇出（fanout）

### 无名exchange

在前面我们对 exchange 一无所知，但仍然能够将消息发送到队列。之前能实现的原因是因为我们使用的是默认交换，我们通过空字符串进行标识。

```java
channel.basicPublish("", queueName, null, msg.getBytes());
```

第一个参数是交换机的名称。空字符串表示默认或无名称交换机：消息能路由发送到队列中其实是由 `routingKey(bindingkey)` 绑定 key 指定的，如果它存在的话。

## 临时队列

之前我们使用的是具有特定名称的队列。队列的名称我们来说至关重要：我们需要指定我们的消费者去消费哪个队列的消息。

每当我们连接到 RabbitMQ 时，我们都需要一个全新的空队列，为此我们可以创建一个具有**随机名称的队列**，或者能让服务器为我们选择一个随机队列名称那就更好了。其次**一旦我们断开了消费者的连接，队列将被自动删除**。

创建临时队列的方式如下：

```java
String queueName = channel.queueDeclare().getQueue();
```

创建出来之后长成这样：

![image-20211017152431523](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211017152431523.png)

## 绑定（bingdings）

binding 其实是 exchange 和 queue 之间的桥梁，它告诉我们 exchange 和哪个队列进行了绑定关系。比如说下面这张图告诉我们的就是 X 与 Q1 和 Q2 进行了绑定：

![image-20211017152607486](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211017152607486.png)

## Fanout

### 介绍

Fanout 这种类型非常简单。它是将接收到的所有消息广播到它知道的所有队列中。系统中默认有些 exchange 类型：

![image-20211017153014796](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211017153014796.png)

### 实战

![image-20211017153659838](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211017153659838.png)

Logs 和临时队列的绑定关系如下图：

![image-20211017154025489](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211017154025489.png)

ReceiveLogs01 和 ReceiveLogs02 将接收到的消息打印在控制台：

```java
public class ReceiveLogs01 {
	public static final String EXCHANGE_NAME = "logs";

	public static void main(String[] args) throws IOException {
		Channel channel = RabbitMqUtils.getChannel();
		//声明一个队列
		/**
		 * 生成一个临时队列，队列的名称是随机的
		 * 当消费者断开与队列的连接时，队列自动删除
		 */
		String queueName = channel.queueDeclare().getQueue();
		//绑定交换机与队列
		channel.queueBind(queueName, EXCHANGE_NAME, "1");
		System.out.println("等待接收消息，把接收到的消息打印在屏幕上......");

		//接收消息
		DeliverCallback deliverCallback = (consumerTag, message) -> {
			System.out.println("ReceiveLogs01 打印消息:" + new String(message.getBody()));
		};

		//消费者取消消费时回调接口
		CancelCallback cancelCallback = consumerTag -> {
			System.out.println(consumerTag + "消费者取消消费接口回调逻辑");
		};

		channel.basicConsume(queueName, true, deliverCallback, cancelCallback);
	}
}
```

EmitLog 发送消息给两个消费者：

```java
public class EmitLog {
	public static final String EXCHANGE_NAME = "logs";

	public static void main(String[] args) throws IOException {
		Channel channel = RabbitMqUtils.getChannel();
		//声明一个交换机
		channel.exchangeDeclare(EXCHANGE_NAME, BuiltinExchangeType.FANOUT);

		Scanner scanner = new Scanner(System.in);
		while (scanner.hasNext()) {
			String msg = scanner.next();
			channel.basicPublish(EXCHANGE_NAME, "", null, msg.getBytes());
			System.out.println("生产者发出消息：" + msg);
		}
	}
}
```

结果：当 EmitLog 发出消息时，向绑定到交换机上的所有队列传递消息，即使`routingKey`不同，也都能接收到相同的消息。

## Direct

### 介绍

direct 这种类型的工作方式是，消息只去到它绑定的 routingKey 队列中去。

![image-20211017173331747](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211017173331747.png)

在上面这张图中，我们可以看到 X 绑定了两个队列，绑定类型是direct。队列 Q1 路由键为 orange，队列 Q2 路由键有两个：一个路由键为 black，另一个路由键为 green。

在这种绑定情况下，生产者发布消息到 exchange 上，路由键为 orange 的消息会被发布到队列 Q1。路由键为 black 和 green 的消息会被发布到队列 Q2，其他消息类型的消息将被丢弃。

### 多重绑定

![image-20211017173707075](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211017173707075.png)

当然如果 exchange 的绑定类型是 direct，但是它绑定的多个队列的 key 如果都相同，在这种情况下虽然绑定类型是 direct 但是它表现的就和 fanout 有点类似了，就跟广播差不多，如上图所示。

### 实战

生产者：

```java
public class DirectLogs {
	public static final String EXCHANGE_NAME = "direct_logs";
	public static final String ROUTING_KEY_INFO = "info";
	public static final String ROUTING_KEY_WARNING = "warning";
	public static final String ROUTING_KEY_ERROR = "error";

	public static void main(String[] args) throws IOException {
		Channel channel = RabbitMqUtils.getChannel();
		//声明一个交换机
		channel.exchangeDeclare(EXCHANGE_NAME, BuiltinExchangeType.DIRECT);

		Scanner scanner = new Scanner(System.in);
		while (scanner.hasNext()) {
			String msg = scanner.next();
			channel.basicPublish(EXCHANGE_NAME, ROUTING_KEY_INFO, null, msg.getBytes());
			System.out.println("生产者发出消息：" + msg);
		}
	}
}
```

消费者1：

```java
public class ReceiveLogsDirect01 {
	public static final String EXCHANGE_NAME = "direct_logs";
	public static final String QUEUE_NAME = "console";
	public static final String ROUTING_KEY_INFO = "info";
	public static final String ROUTING_KEY_WARNING = "warning";

	public static void main(String[] args) throws IOException {
		Channel channel = RabbitMqUtils.getChannel();
		//声明一个队列
		channel.queueDeclare(QUEUE_NAME, false, false, false, null);
		//绑定两个routingKey
		channel.queueBind(QUEUE_NAME, EXCHANGE_NAME, ROUTING_KEY_INFO);
		channel.queueBind(QUEUE_NAME, EXCHANGE_NAME, ROUTING_KEY_WARNING);

		//接收消息
		DeliverCallback deliverCallback = (consumerTag, message) -> {
			System.out.println("ReceiveLogsDirect01 打印消息:" + new String(message.getBody()));
		};

		//消费者取消消费时回调接口
		CancelCallback cancelCallback = consumerTag -> {
			System.out.println(consumerTag + "消费者取消消费接口回调逻辑");
		};

		channel.basicConsume(QUEUE_NAME, true, deliverCallback, cancelCallback);
	}
}
```

消费者2（绑定的 routingKey 不同）：

```java
public class ReceiveLogsDirect02 {
	public static final String EXCHANGE_NAME = "direct_logs";
	public static final String QUEUE_NAME = "disk";
	public static final String ROUTING_KEY_ERROR = "error";

	public static void main(String[] args) throws IOException {
		Channel channel = RabbitMqUtils.getChannel();
		//声明一个队列
		channel.queueDeclare(QUEUE_NAME, false, false, false, null);
		//绑定一个routingKey
		channel.queueBind(QUEUE_NAME, EXCHANGE_NAME, ROUTING_KEY_ERROR);

		//接收消息
		DeliverCallback deliverCallback = (consumerTag, message) -> {
			System.out.println("ReceiveLogsDirect02 打印消息:" + new String(message.getBody()));
		};

		//消费者取消消费时回调接口
		CancelCallback cancelCallback = consumerTag -> {
			System.out.println(consumerTag + "消费者取消消费接口回调逻辑");
		};

		channel.basicConsume(QUEUE_NAME, true, deliverCallback, cancelCallback);
	}
}
```

结果：

- 生产者向`ROUTING_KEY_INFO`发送消息时，消费者1可以收到消息
- 生产者向`ROUTING_KEY_WARNING`发送消息时，消费者1可以收到消息
- 生产者向`ROUTING_KEY_ERROR`发送消息时，消费者2可以收到消息
- 如果启动多个消费者1，生产者向`ROUTING_KEY_INFO`或`ROUTING_KEY_WARNING`发送消息时，多个消费者1将会轮流公平接收消息，且每个消息只会被消费一次。

## Topic

### 要求

发送到类型是 topic 交换机的消息的 routing_key 不能随意写，必须满足一定的要求，**它必须是一个单词列表，以点号分隔开**。这些单词可以是任意单词，比如："stock.usd.nyse", "nyse.vmw", "quick.orange.rabbit"。当然这个单词列表最多不能超过255个字节。

在这个规则列表中，其中有两个替换符：

- \*(星号)可以代替一个单词
- \#(井号)可以替代零个或多个单词

### 匹配案例

下图绑定关系如下

Q1 绑定的是：中间带 orange 带3个单词的字符串(\*.orange.\*)
Q2 绑定的是：最后一个单词是 rabbit 的3个单词(\*.\*.rabbit)、第一个单词是 lazy 的多个单词(lazy.#)

![image-20211017184407171](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211017184407171.png)

上图是一个队列绑定关系图，我们来看看他们之间数据接收情况是怎么样的

- quick.orange.rabbit 被队列 Q1 Q2 接收到
- lazy.orange.elephant 被队列 Q1 Q2 接收到
- quick.orange.fox 被队列 Q1 接收到
- lazy.brown.fox 被队列 Q2 接收到
- lazy.pink.rabbit 虽然满足两个绑定但只被队列 Q2 接收一次
- quick.brown.fox 不匹配任何绑定不会被任何队列接收到会被丢弃
- quick.orange.male.rabbit 是四个单词不匹配任何绑定会被丢弃
- lazy.orange.male.rabbit 是四个单词但匹配 Q2

当队列绑定关系是下列这种情况时需要引起注意：

- 当一个队列绑定键是 #，那么这个队列将接收所有数据，就有点像 fanout 了
- 如果队列绑定键当中没有 # 和 * 出现，那么该队列绑定类型就是 direct 了

### 实战

![image-20211017185031814](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211017185031814.png)

消费者1：

```java
public class ReceiveLogsTopic01 {
	public static final String EXCHANGE_NAME = "topic_logs";
	public static final String QUEUE_NAME = "Q1";
	public static final String ROUTING_KEY = "*.orange.*";

	public static void main(String[] args) throws IOException {
		Channel channel = RabbitMqUtils.getChannel();
		//声明一个交换机
		channel.exchangeDeclare(EXCHANGE_NAME, BuiltinExchangeType.TOPIC);
		//声明一个队列
		channel.queueDeclare(QUEUE_NAME, false, false, false, null);
		//绑定一个routingKey
		channel.queueBind(QUEUE_NAME, EXCHANGE_NAME, ROUTING_KEY);
		System.out.println("等待接收消息......");

		//接收消息
		DeliverCallback deliverCallback = (consumerTag, message) -> {
			System.out.println("queue:" + QUEUE_NAME + ", routingKey:" + message.getEnvelope().getRoutingKey() + ", msg:" + new String(message.getBody()));
		};

		//消费者取消消费时回调接口
		CancelCallback cancelCallback = consumerTag -> {
			System.out.println(consumerTag + "消费者取消消费接口回调逻辑");
		};

		channel.basicConsume(QUEUE_NAME, true, deliverCallback, cancelCallback);
	}
}
```

消费者2：

```java
public class ReceiveLogsTopic02 {
	public static final String EXCHANGE_NAME = "topic_logs";
	public static final String QUEUE_NAME = "Q2";
	public static final String ROUTING_KEY1 = "*.*.rabbit";
	public static final String ROUTING_KEY2 = "lazy.#";

	public static void main(String[] args) throws IOException {
		Channel channel = RabbitMqUtils.getChannel();
		//声明一个交换机
		channel.exchangeDeclare(EXCHANGE_NAME, BuiltinExchangeType.TOPIC);
		//声明一个队列
		channel.queueDeclare(QUEUE_NAME, false, false, false, null);
		//绑定一个routingKey
		channel.queueBind(QUEUE_NAME, EXCHANGE_NAME, ROUTING_KEY1);
		channel.queueBind(QUEUE_NAME, EXCHANGE_NAME, ROUTING_KEY2);
		System.out.println("等待接收消息......");

		//接收消息
		DeliverCallback deliverCallback = (consumerTag, message) -> {
			System.out.println("queue:" + QUEUE_NAME + ", routingKey:" + message.getEnvelope().getRoutingKey() + ", msg:" + new String(message.getBody()));
		};

		//消费者取消消费时回调接口
		CancelCallback cancelCallback = consumerTag -> {
			System.out.println(consumerTag + "消费者取消消费接口回调逻辑");
		};

		channel.basicConsume(QUEUE_NAME, true, deliverCallback, cancelCallback);
	}
}
```

生产者：

```java
public class EmitLogTopic {
	public static final String EXCHANGE_NAME = "topic_logs";

	public static void main(String[] args) throws IOException {
		Channel channel = RabbitMqUtils.getChannel();

		Map<String, String> bindingKeyMap = new LinkedHashMap<>();
		bindingKeyMap.put("quick.orange.rabbit", "被队列 Q1 Q2 接收到");
		bindingKeyMap.put("lazy.orange.elephant", "被队列 Q1 Q2 接收到");
		bindingKeyMap.put("quick.orange.fox", "被队列 Q1 接收到");
		bindingKeyMap.put("lazy.brown.fox", "被队列 Q2 接收到");
		bindingKeyMap.put("lazy.pink.rabbit", "虽然满足两个绑定但只被队列 Q2 接收一次");
		bindingKeyMap.put("quick.brown.fox", "不匹配任何绑定不会被任何队列接收到会被丢弃");
		bindingKeyMap.put("quick.orange.male.rabbit", "是四个单词不匹配任何绑定会被丢弃");
		bindingKeyMap.put("lazy.orange.male.rabbit", "是四个单词但匹配 Q2");

		bindingKeyMap.forEach((k, v) -> {
			try {
				channel.basicPublish(EXCHANGE_NAME, k, null, v.getBytes());
			} catch (IOException e) {
				e.printStackTrace();
			}
			System.out.println(k + v);
		});

	}
}
```

结果：

```java
---------------------
EmitLogTopic:
---------------------
quick.orange.rabbit被队列 Q1 Q2 接收到
lazy.orange.elephant被队列 Q1 Q2 接收到
quick.orange.fox被队列 Q1 接收到
lazy.brown.fox被队列 Q2 接收到
lazy.pink.rabbit虽然满足两个绑定但只被队列 Q2 接收一次
quick.brown.fox不匹配任何绑定不会被任何队列接收到会被丢弃
quick.orange.male.rabbit是四个单词不匹配任何绑定会被丢弃
lazy.orange.male.rabbit是四个单词但匹配 Q2
---------------------
ReceiveLogsTopic01:
---------------------
等待接收消息......
queue:Q1, routingKey:quick.orange.rabbit, msg:被队列 Q1 Q2 接收到
queue:Q1, routingKey:lazy.orange.elephant, msg:被队列 Q1 Q2 接收到
queue:Q1, routingKey:quick.orange.fox, msg:被队列 Q1 接收到
---------------------
ReceiveLogsTopic02:
---------------------
等待接收消息......
queue:Q2, routingKey:quick.orange.rabbit, msg:被队列 Q1 Q2 接收到
queue:Q2, routingKey:lazy.orange.elephant, msg:被队列 Q1 Q2 接收到
queue:Q2, routingKey:lazy.brown.fox, msg:被队列 Q2 接收到
queue:Q2, routingKey:lazy.pink.rabbit, msg:虽然满足两个绑定但只被队列 Q2 接收一次
queue:Q2, routingKey:lazy.orange.male.rabbit, msg:是四个单词但匹配 Q2
```

# 死信队列

## 概念

死信，顾名思义就是无法被消费的消息，字面意思可以这样理解，一般来说，producer 将消息投递到 broker 或者直接到 queue 里了，consumer 从 queue 取出消息进行消费，但某些时候由于特定的原因**导致 queue 中的某些消息无法被消费**，这样的消息如果没有后续的处理，就变成了死信，有死信自然就有了死信队列。

应用场景：为了保证订单业务的消息数据不丢失，需要使用到 RabbitMQ 的死信队列机制，当消息消费发生异常时，将消息投入死信队列中。还有比如说：用户在商城下单成功并点击去支付后在指定时间未支付时自动失效。

## 来源

- 消息 TTL 过期
- 队列达到最大长度（队列满了，无法再添加数据到 mq 中）
- 消息被拒绝（basic.reject 或 basic.nack）并且 requeue=false（不再放回队列中）

## 实战

消费者1：

```java
public class Consumer01 {
	//普通交换机名称
	public static final String NORMAL_EXCHANGE = "normal_exchange";
	//死信交换机名称
	public static final String DEAD_EXCHANGE = "dead_exchange";
	//普通队列名称
	public static final String NORMAL_QUEUE = "normal_queue";
	//死信队列名称
	public static final String DEAD_QUEUE = "dead_queue";
	//普通队列routingKey
	public static final String NORMAL_ROUTING_KEY = "normal_routing_key";
	//死信队列routingKey
	public static final String DEAD_ROUTING_KEY = "dead_routing_key";

	public static void main(String[] args) throws IOException {
		Channel channel = RabbitMqUtils.getChannel();
		//声明死信和普通交换机
		channel.exchangeDeclare(NORMAL_EXCHANGE, BuiltinExchangeType.DIRECT);
		channel.exchangeDeclare(DEAD_EXCHANGE, BuiltinExchangeType.DIRECT);

		//设置普通队列参数
		Map<String, Object> arguments = new HashMap<>();
		//设置队列中消息过期时间 或在生产者指定消息过期时间
//		arguments.put("x-message-ttl", 10000);
		//正常队列设置死信交换机
		arguments.put("x-dead-letter-exchange", DEAD_EXCHANGE);
		//设置死信routingKey
		arguments.put("x-dead-letter-routing-key", DEAD_ROUTING_KEY);
		//设置普通队列长度的限制
//		arguments.put("x-max-length", 6);

		//声明普通队列
		channel.queueDeclare(NORMAL_QUEUE, false, false, false, arguments);

		//声明死信队列
		channel.queueDeclare(DEAD_QUEUE, false, false, false, null);

		//绑定普通交换机和普通队列
		channel.queueBind(NORMAL_QUEUE, NORMAL_EXCHANGE, NORMAL_ROUTING_KEY);
		//绑定死信交换机和死信队列
		channel.queueBind(DEAD_QUEUE, DEAD_EXCHANGE, DEAD_ROUTING_KEY);

		//接收消息
		DeliverCallback deliverCallback = (consumerTag, message) -> {
			System.out.println(new String(message.getBody()));
//			if (message.getEnvelope().getDeliveryTag() == 5) {
//				System.out.println("拒绝消息:" + new String(message.getBody()));
//				channel.basicReject(message.getEnvelope().getDeliveryTag(), false);
//			} else {
//				System.out.println("接收消息:" + new String(message.getBody()));
//				channel.basicAck(message.getEnvelope().getDeliveryTag(), false);
//			}
		};

		//消费者取消消费时回调接口
		CancelCallback cancelCallback = consumerTag -> {
			System.out.println(consumerTag + "消费者取消消费接口回调逻辑");
		};

		channel.basicConsume(NORMAL_QUEUE, true, deliverCallback, cancelCallback);
	}
}
```

生产者：

```java
public class Producer {
	//普通交换机名称
	public static final String NORMAL_EXCHANGE = "normal_exchange";
	//普通队列routingKey
	public static final String NORMAL_ROUTING_KEY = "normal_routing_key";

	public static void main(String[] args) throws IOException {
		Channel channel = RabbitMqUtils.getChannel();

		//死信消息 设置TTL时间
		AMQP.BasicProperties basicProperties = new AMQP.BasicProperties()
				.builder()
				.expiration("10000")
				.build();

		for (int i = 0; i < 10; i++) {
			String msg = i + "";
			channel.basicPublish(NORMAL_EXCHANGE, NORMAL_ROUTING_KEY, basicProperties, msg.getBytes());
			System.out.println("生产者发出消息：" + msg);
		}
	}
}
```

消费者2：

```java
public class Consumer02 {
	//死信队列名称
	public static final String DEAD_QUEUE = "dead_queue";

	public static void main(String[] args) throws IOException {
		Channel channel = RabbitMqUtils.getChannel();

		//接收消息
		DeliverCallback deliverCallback = (consumerTag, message) -> {
			System.out.println(new String(message.getBody()));
		};

		//消费者取消消费时回调接口
		CancelCallback cancelCallback = consumerTag -> {
			System.out.println(consumerTag + "消费者取消消费接口回调逻辑");
		};

		channel.basicConsume(DEAD_QUEUE, true, deliverCallback, cancelCallback);
	}
}
```

1. 测试消息 TTL 过期
   1. 启动消费者1，创建普通队列和死信队列，然后关闭消费者1，模拟其收不到消息
   2. 启动生产者往普通队列中发送消息，消息在达到 TTL 后，由于没有被消费者消费，自动进入死信队列
   3. 启动消费者2，将死信队列中的死信消费完毕
2. 测试队列达到最大长度
   1. 删除现有的普通队列，在普通队列参数中添加`arguments.put("x-max-length", 6);`设置队列长度
   2. 启动消费者1，创建普通队列和死信队列，然后关闭消费者1，模拟其收不到消息
   3. 在生产者处取消设置消息 TTL
   4. 启动生产者往普通队列中发送10条消息，后4条消息由于超过队列长度6的限制，自动进入死信队列
   5. 启动消费者2，将死信队列中的死信消费完毕
3. 测试消息被拒绝
   1. 删除现有的普通队列，在普通队列参数中取消队列长度限制
   2. 在`basicConsume`中设置为手动应答，并在接收消息回调中拒绝`tag==5`的消息，不重新放回队列中
   3. 启动消费者1，创建普通队列和死信队列
   4. 启动消费者2
   5. 启动生产者往普通队列中发送10条消息
   6. 第5条消息由于被消费者1拒绝，直接投递到死信队列，被消费者2消费

# 延迟队列

## 概念

延迟队列，队列内部是有序的，最重要的特性就体现在它的延时属性上，延迟队列中的元素是希望在指定时间到了以后或之前取出和处理，简单来说，延迟队列就是用来存放需要在指定时间被处理的元素的队列。

## 使用场景

1. 订单在十分钟之内未支付则自动取消
2. 新创建的店铺，如果在十天内都没有上传过商品，则自动发送消息提醒
3. 用户注册成功后，如果三天内没有登陆则进行短信提醒

这些场景都有一个特点，需要在某个事件发生之后或者之前的指定时间点完成某一项任务，如：发生订单生成事件，在十分钟之后检查该订单支付状态，然后将未支付的订单进行关闭。看起来似乎使用定时任务，一直轮询数据，每秒查一次，取出需要被处理的数据，然后处理不就完事了吗？如果数据量比较少，确实可以这样做，比如：对于“如果账单一周内未支付则进行自动结算”这样的需求，如果对于时间不是严格限制，而是宽松意义上的一周，那么每天晚上跑个定时任务检查一下所有未支付的账单，确实也是一个可行的方案。但对于数据量比较大，并且时效性较强的场景，如：“订单十分钟内未支付则关闭“，短期内未支付的订单数据可能会有很多，活动期间甚至会达到百万甚至千万级别，对这么庞大的数据量仍旧使用轮询的方式显然是不可取的，很可能在一秒内无法完成所有订单的检查，同时会给数据库带来很大压力，无法满足业务要求而且性能低下。

## 两种设置消息TTL的方式

1. 在声明队列时，设置队列属性中消息过期时间

```java
Map<String, Object> arguments = new HashMap<>();
arguments.put("x-message-ttl", 10000);
channel.queueDeclare(NORMAL_QUEUE, false, false, false, arguments);
```

2. 在发布消息时，设置消息本身的过期时间

```java
AMQP.BasicProperties basicProperties = new AMQP.BasicProperties().builder().expiration("10000").build();
channel.basicPublish(NORMAL_EXCHANGE, NORMAL_ROUTING_KEY, basicProperties, msg.getBytes());
```

**两种方式的区别：**

1. 设置队列属性， 那么队列中所有的消息都有相同的过期时间，如果消息过期，则会直接从队列中丢弃，原因是，由于 TTL 一致， 又是 FIFO 的模式，那么过期消息一定在队列的头部
2. 对于消息本身设置 TTL，如果消息过期，不会马上丢弃，而是在消息进行投递的时候检查过期再丢弃，这样1. 简单，维护成本低 2. 性能更好，不用实时扫描队列

## 队列TTL-整合Spring Boot

### 代码结构图

创建两个队列 QA 和 QB，两者队列 TTL 分别设置为 10s 和 40s，然后再创建一个交换机 X 和死信交换机 Y，它们的类型都是 direct，创建一个死信队列 QD，它们的绑定关系如下：

![image-20211019203406439](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211019203406439.png)

### 代码

声明交换机和队列：

```java
@Configuration
public class TtlQueueConfig {
	//普通交换机名称
	public static final String EXCHANGE_X = "X";
	//死信交换机名称
	public static final String DEAD_LETTER_EXCHANGE_Y = "Y";
	//普通队列名称
	public static final String QUEUE_A = "QA";
	public static final String QUEUE_B = "QB";
	//死信队列名称
	public static final String DEAD_LETTER_QUEUE_D = "QD";
	//普通队列A routingKey
	public static final String QUEUE_A_ROUTING_KEY = "XA";
	//普通队列B routingKey
	public static final String QUEUE_B_ROUTING_KEY = "XB";
	//死信队列D routingKey
	public static final String DEAD_LETTER_QUEUE_D_ROUTING_KEY = "YD";

	/**
	 * 声明交换机X
	 */
	@Bean("exchangeX")
	public DirectExchange exchangeX() {
		return new DirectExchange(EXCHANGE_X);
	}

	/**
	 * 声明交换机Y
	 */
	@Bean("exchangeY")
	public DirectExchange exchangeY() {
		return new DirectExchange(DEAD_LETTER_EXCHANGE_Y);
	}

	/**
	 * 声明普通队列QA TTL为10s
	 */
	@Bean("queueA")
	public Queue queueA() {
		Map<String, Object> arguments = new HashMap<>();
		//设置死信交换机
		arguments.put("x-dead-letter-exchange", DEAD_LETTER_EXCHANGE_Y);
		//设置死信routingKey
		arguments.put("x-dead-letter-routing-key", DEAD_LETTER_QUEUE_D_ROUTING_KEY);
		//设置TTL 单位ms
		arguments.put("x-message-ttl", 10000);
		return QueueBuilder.durable(QUEUE_A).withArguments(arguments).build();
	}

	/**
	 * 声明普通队列QB TTL为40s
	 */
	@Bean("queueB")
	public Queue queueB() {
		Map<String, Object> arguments = new HashMap<>();
		//设置死信交换机
		arguments.put("x-dead-letter-exchange", DEAD_LETTER_EXCHANGE_Y);
		//设置死信routingKey
		arguments.put("x-dead-letter-routing-key", DEAD_LETTER_QUEUE_D_ROUTING_KEY);
		//设置TTL 单位ms
		arguments.put("x-message-ttl", 40000);
		return QueueBuilder.durable(QUEUE_B).withArguments(arguments).build();
	}

	/**
	 * 声明死信队列
	 */
	@Bean("queueD")
	public Queue queueD() {
		return QueueBuilder.durable(DEAD_LETTER_QUEUE_D).build();
	}

	/**
	 * 绑定QA和X
	 */
	@Bean
	public Binding queueABindingX(@Qualifier("queueA") Queue queueA, @Qualifier("exchangeX") DirectExchange exchangeX) {
		return BindingBuilder.bind(queueA).to(exchangeX).with(QUEUE_A_ROUTING_KEY);
	}

	/**
	 * 绑定QB和X
	 */
	@Bean
	public Binding queueBBindingX(@Qualifier("queueB") Queue queueB, @Qualifier("exchangeX") DirectExchange exchangeX) {
		return BindingBuilder.bind(queueB).to(exchangeX).with(QUEUE_B_ROUTING_KEY);
	}

	/**
	 * 绑定QD和Y
	 */
	@Bean
	public Binding queueDBindingY(@Qualifier("queueD") Queue queueD, @Qualifier("exchangeY") DirectExchange exchangeY) {
		return BindingBuilder.bind(queueD).to(exchangeY).with(DEAD_LETTER_QUEUE_D_ROUTING_KEY);
	}
}
```

生产者：

```java
@Slf4j
@RestController
@RequestMapping("/ttl")
public class SendMsgController {
	@Autowired
	private RabbitTemplate rabbitTemplate;

	@GetMapping("/sendMsg/{message}")
	public void sendMsg(@PathVariable String message) {
		log.info("当前时间:{}, 发送一条消息给两个TTL队列:{}", LocalDateTime.now(), message);
		rabbitTemplate.convertAndSend(TtlQueueConfig.EXCHANGE_X, TtlQueueConfig.QUEUE_A_ROUTING_KEY, "消息来自TTL为10s的队列:" + message);
		rabbitTemplate.convertAndSend(TtlQueueConfig.EXCHANGE_X, TtlQueueConfig.QUEUE_B_ROUTING_KEY, "消息来自TTL为40s的队列:" + message);
	}
}
```

死信队列消费者：

```java
@Slf4j
@Component
public class DeadLetterQueueConsumer {
	@RabbitListener(queues = TtlQueueConfig.DEAD_LETTER_QUEUE_D)
	public void receiveD(Message message, Channel channel) {
		String msg = new String(message.getBody());
		log.info("当前时间:{}, 收到死信队列的消息:{}", LocalDateTime.now(), msg);
	}
}
```

**第一条消息在 10s 后变成了死信消息，然后被消费者消费掉，第二条消息在 40s 之后变成了死信消息，然后被消费掉，这样一个延迟队列就打造完成了。**

## 延迟队列优化

不过，如果这样使用的话，岂不是每增加一个新的时间需求，就要新增一个队列，这里只有 10s 和 40s 两个时间选项，如果需要一个小时后处理，那么就需要增加 TTL 为一个小时的队列，如果是预定会议室然后提前通知这样的场景，岂不是要增加无数个队列才能满足需求？

### 代码结构图

在这里新增了一个队列 QC，绑定关系如下，该队列不设置 TTL 时间，而是在生产者发送消息时设置消息 TTL

![image-20211020174913453](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211020174913453.png)

### 代码

声明队列 QC：

```java
public static final String QUEUE_C = "QC";
public static final String QUEUE_C_ROUTING_KEY = "XC";
/**
 * 声明普通队列QC 不设置TTL
 */
@Bean("queueC")
public Queue queueC() {
	Map<String, Object> arguments = new HashMap<>();
	//设置死信交换机
	arguments.put("x-dead-letter-exchange", DEAD_LETTER_EXCHANGE_Y);
	//设置死信routingKey
	arguments.put("x-dead-letter-routing-key", DEAD_LETTER_QUEUE_D_ROUTING_KEY);
	return QueueBuilder.durable(QUEUE_C).withArguments(arguments).build();
}
/**
 * 绑定QC和X
 */
@Bean
public Binding queueCBindingX(@Qualifier("queueC") Queue queueC, @Qualifier("exchangeX") DirectExchange exchangeX) {
	return BindingBuilder.bind(queueC).to(exchangeX).with(QUEUE_C_ROUTING_KEY);
}
```

生产者：

```java
@GetMapping("/sendExpirationMsg/{message}/{ttlTime}")
public void sendMsg(@PathVariable String message, @PathVariable String ttlTime) {
	log.info("当前时间:{}, 发送一条TTL为{}ms的消息给队列QC:{}", LocalDateTime.now(), ttlTime, message);
	rabbitTemplate.convertAndSend(TtlQueueConfig.EXCHANGE_X, TtlQueueConfig.QUEUE_C_ROUTING_KEY, message, msg -> {
		//设置消息TTL
		msg.getMessageProperties().setExpiration(ttlTime);
		return msg;
	});
}
```

**看起来似乎没什么问题，但是在[两种设置消息TTL的方式](#两种设置消息ttl的方式)，介绍过如果使用在消息属性上设置 TTL 的方式，消息可能并不会按时进入死信队列，因为 RabbitMQ 只会检查队列中第一个消息是否过期，如果过期则丢到死信队列，如果第一个消息的延时时长很长，而第二个消息的延时时长很短，第二个消息并不会优先得到执行。**

## 插件实现延迟队列

下载 RabbitMQ 延迟队列插件 [rabbitmq-delayed-message-exchange](https://github.com/rabbitmq/rabbitmq-delayed-message-exchange)，并拷贝至`/usr/lib/rabbitmq/lib/rabbitmq_server-3.9.8/plugins/`目录下，启用插件

```shell
rabbitmq-plugins enable rabbitmq_delayed_message_exchange
```

看到这个选项说明插件安装成功

![image-20211021164531535](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211021164531535.png)

使用这种类型的交换机后，**消息的延迟将从队列转移到交换机**

### 代码结构图

在这里新增了一个队列 delayed.queue，一个自定义交换机 delayed.exchange，绑定关系如下：

![image-20211021165257613](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211021165257613.png)

### 代码

声明队列和交换机：

```java
@Configuration
public class DelayedQueueConfig {
	//交换机
	public static final String DELAYED_EXCHANGE_NAME = "delayed.exchange";
	//队列
	public static final String DELAYED_QUEUE_NAME = "delayed.queue";
	//routingKey
	public static final String DELAYED_ROUTING_KEY = "delayed.routingkey";

	/**
	 * 声明基于直接类型的延迟交换机
	 */
	@Bean
	public CustomExchange delayedExchange() {
		Map<String, Object> arguments = new HashMap<>();
		arguments.put("x-delayed-type", "direct");
		/**
		 * 1.交换机的名称
		 * 2.交换机的类型
		 * 3.是否需要持久化
		 * 4.是否需要自动删除
		 * 5.其它参数
		 */
		return new CustomExchange(DELAYED_EXCHANGE_NAME, "x-delayed-message", true, false, arguments);
	}

	/**
	 * 声明队列
	 */
	@Bean
	public Queue delayedQueue() {
		return new Queue(DELAYED_QUEUE_NAME);
	}

	/**
	 * 绑定队列和交换机
	 */
	@Bean
	public Binding delayedQueueBindingDelayedExchange(@Qualifier("delayedQueue") Queue delayedQueue,
	                                                  @Qualifier("delayedExchange") CustomExchange delayedExchange) {
		return BindingBuilder.bind(delayedQueue).to(delayedExchange).with(DELAYED_ROUTING_KEY).noargs();
	}
}
```

生产者：

```java
@GetMapping("/sendDelayMsg/{message}/{delayTime}")
public void sendMsg(@PathVariable String message, @PathVariable Integer delayTime) {
	log.info("当前时间:{}, 发送一条TTL为{}ms的消息给延迟队列delayed.queue:{}", LocalDateTime.now(), delayTime, message);
	rabbitTemplate.convertAndSend(DelayedQueueConfig.DELAYED_EXCHANGE_NAME, DelayedQueueConfig.DELAYED_ROUTING_KEY, message, msg -> {
		//发送消息时 设置延迟时长 单位ms
		msg.getMessageProperties().setDelay(delayTime);
		return msg;
	});
}
```

消费者：

```java
@Slf4j
@Component
public class DelayQueueConsumer {
	@RabbitListener(queues = DelayedQueueConfig.DELAYED_QUEUE_NAME)
	public void receiveD(Message message, Channel channel) {
		String msg = new String(message.getBody());
		log.info("当前时间:{}, 收到延迟队列的消息:{}", LocalDateTime.now(), msg);
	}
}
```

**发起两个请求，第一个延迟时间较长，第二个延迟时间短，第二个消息被先消费掉了，符合预期**

## 总结

延迟队列在需要延时处理的场景下非常有用，使用 RabbitMQ 来实现延迟队列可以很好的利用 RabbitMQ 的特性，如：消息可靠发送、消息可靠投递、死信队列来保障消息至少被消费一次以及未被正确处理的消息不会被丢弃。另外，通过 RabbitMQ 集群的特性，可以很好的解决单点故障问题，不会因为单个节点挂掉导致延迟队列不可用或者消息丢失。

当然，延时队列还有很多其它选择，比如利用 Java 的 DelayQueue，利用 Redis 的 zset，利用 Quartz 或者利用 kafka 的时间轮，这些方式各有特点，看需要适用的场景

# 发布确认高级

在生产环境中由于一些不明原因，导致 RabbitMQ 重启，在 RabbitMQ 重启期间生产者消息投递失败，导致消息丢失，需要手动处理和恢复。于是，我们开始思考，如何才能进行 RabbitMQ 的消息可靠投递呢？特别是在这样比较极端的情况，RabbitMQ 集群不可用的时候，无法投递的消息该如何处理呢

## 发布确认Spring Boot版本

### 代码结构图

![image-20211021210521339](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211021210521339.png)

### 配置文件

在配置文件`application.properties`中需要添加

```xml
spring.rabbitmq.publisher-confirm-type=correlated
```

- none：禁用发布确认模式，是默认值
- correlated：类似异步确认发布，发布消息成功到交换机后会触发回调方法
- simple：类似单独确认发布

### 声明交换机和队列

```java
@Configuration
public class ConfirmConfig {
	public static final String CONFIRM_EXCHANGE_NAME = "confirm_exchange";
	public static final String CONFIRM_QUEUE_NAME = "confirm_queue";
	public static final String CONFIRM_ROUTING_KEY = "key1";

	@Bean
	public DirectExchange confirmExchange() {
		return new DirectExchange(CONFIRM_EXCHANGE_NAME);
	}

	@Bean
	public Queue confirmQueue() {
		return QueueBuilder.durable(CONFIRM_QUEUE_NAME).build();
	}

	@Bean
	public Binding queueBindingExchange(@Qualifier("confirmExchange") DirectExchange confirmExchange,
	                                    @Qualifier("confirmQueue") Queue confirmQueue) {
		return BindingBuilder.bind(confirmQueue).to(confirmExchange).with(CONFIRM_ROUTING_KEY);
	}
}
```

### 生产者

```java
@Slf4j
@RestController
@RequestMapping("/confirm")
public class ProducerController {
	@Autowired
	private RabbitTemplate rabbitTemplate;

	@GetMapping("/sendMsg/{message}")
	public void sendMsg(@PathVariable String message) {
		CorrelationData correlationData1 = new CorrelationData("1");
		rabbitTemplate.convertAndSend(ConfirmConfig.CONFIRM_EXCHANGE_NAME, ConfirmConfig.CONFIRM_ROUTING_KEY, message + "1", correlationData1);
		log.info("发送消息内容：{}", message + "1");

		CorrelationData correlationData2 = new CorrelationData("2");
		rabbitTemplate.convertAndSend(ConfirmConfig.CONFIRM_EXCHANGE_NAME + "?", ConfirmConfig.CONFIRM_ROUTING_KEY, message + "2", correlationData2);
		log.info("发送消息内容：{}", message + "2");

		CorrelationData correlationData3 = new CorrelationData("3");
		rabbitTemplate.convertAndSend(ConfirmConfig.CONFIRM_EXCHANGE_NAME, ConfirmConfig.CONFIRM_ROUTING_KEY + "?", message + "3", correlationData3);
		log.info("发送消息内容：{}", message + "3");
	}
}
```

### 消费者

```java
@Slf4j
@Component
public class ConfirmConsumer {
	@RabbitListener(queues = ConfirmConfig.CONFIRM_QUEUE_NAME)
	public void receiveConfirmMessage(Message message) {
		String msg = new String(message.getBody());
		log.info("接收到队列confirm.queue消息：{}", msg);
	}
}
```

### 回调接口

```java
@Slf4j
@Component
public class ConfirmCallback implements RabbitTemplate.ConfirmCallback {
	@Autowired
	RabbitTemplate rabbitTemplate;

	@PostConstruct
	public void init() {
		//注入
		rabbitTemplate.setConfirmCallback(this);
	}

	/**
	 * 交换机确认回调接口
	 * 这个回调可以确认消息是否到达交换机
	 *
	 * @param correlationData 保存回调消息的id及相关信息
	 * @param ack             交换机是否收到消息
	 * @param cause           接收消息失败的原因
	 */
	@Override
	public void confirm(CorrelationData correlationData, boolean ack, String cause) {
		String id = correlationData != null ? correlationData.getId() : "null";
		if (ack) {
			log.info("交换机已经收到id为:{}的消息", id);
		} else {
			log.error("交换机未收到id为:{}的消息，原因:{}", id, cause);
		}
	}
}
```

### 结果分析

```java
发送消息内容：消息1
交换机已经收到id为:1的消息
接收到队列confirm.queue消息：消息1
发送消息内容：消息1
发送消息内容：消息1
Shutdown Signal: channel error; protocol method: #method<channel.close>(reply-code=404, reply-text=NOT_FOUND - no exchange 'confirm_exchange?' in vhost '/', class-id=60, method-id=40)
交换机已经收到id为:3的消息
交换机未收到id为:2的消息，原因:channel error; protocol method: #method<channel.close>(reply-code=404, reply-text=NOT_FOUND - no exchange 'confirm_exchange?' in vhost '/', class-id=60, method-id=40)
```

**调用生产者发送三个消息，第一个消息成功被处理。第二个消息因为交换机名称错误，成功被回调接口处理。第三个消息即使 routingKey 错误，但消息已经到达交换机，交换机便返回了 true 的 ack。**

## 回退消息

**在仅开启了生产者确认机制的情况下，交换机接收到消息后，会直接给消息生产者发送确认消息，如果发现该消息不可路由，那么消息会被直接丢弃，此时生产者是不知道消息被丢弃这个事件的**。那么如何让无法被路由的消息帮我想办法处理一下？最起码通知我一声，我好自己处理啊。

在原先不使用 Spring Boot 时，通过发布消息时`channel.basicPublish()`设置`mandatory`参数为`true`，并添加消息回退监听器`channel.addReturnListener()`，可以在当消息传递过程中不可达目的地时将消息返回给生产者。

Spring Boot 版本写法如下（或者`rabbitTemplate.setMandatory(true);`）：

### 配置文件

在配置文件`application.properties`中需要添加

```xml
spring.rabbitmq.publisher-returns=true
```

### 回调接口

实现`RabbitTemplate.ReturnsCallback`接口

```java
@Slf4j
@Component
public class ConfirmCallback implements RabbitTemplate.ConfirmCallback, RabbitTemplate.ReturnsCallback {
	@Autowired
	RabbitTemplate rabbitTemplate;

	@PostConstruct
	public void init() {
		//也可以通过这种方式替代 spring.rabbitmq.publisher-returns=true
//		rabbitTemplate.setMandatory(true);
		//注入
		rabbitTemplate.setConfirmCallback(this);
		rabbitTemplate.setReturnsCallback(this);
	}

	/**
	 * 交换机确认回调接口
	 * 这个回调可以确认消息是否到达交换机
	 *
	 * @param correlationData 保存回调消息的id及相关信息
	 * @param ack             交换机是否收到消息
	 * @param cause           接收消息失败的原因
	 */
	@Override
	public void confirm(CorrelationData correlationData, boolean ack, String cause) {
		String id = correlationData != null ? correlationData.getId() : "null";
		if (ack) {
			log.info("交换机已经收到id为:{}的消息", id);
		} else {
			log.error("交换机未收到id为:{}的消息，原因:{}", id, cause);
		}
	}

	/**
	 * 可以在当消息传递过程中不可达目的地时将消息返回给生产者，只有在消息不可达目的地的时候才进行回退
	 * 这个回调可以确认消息是否到达队列
	 *
	 * @param returned 回退的消息
	 */
	@Override
	public void returnedMessage(ReturnedMessage returned) {
		log.error("消息:{}，被交换机{}退回，原因:{}，路由Key:{}",
				new String(returned.getMessage().getBody()),
				returned.getExchange(),
				returned.getReplyText(),
				returned.getRoutingKey()
		);
	}
}
```

### 结果分析

**调用生产者发送三个消息，第一个消息成功被处理。第二个消息因为交换机名称错误，成功被交换机确认回调接口处理。第三个消息 routingKey 错误，但消息已经到达交换机，被交换机回退消息接口处理。**

Ps. 如果是基于插件的延迟交换机类型`x-delayed-message`，发送带延迟的消息，被交换机接收后，即使 routingKey 和队列都正确，**也会触发交换机回退消息接口**，但消息在延迟后**能够正常被消费者消费**。

## 备份交换机

有了 mandatory 参数和回退消息，我们获得了对无法投递消息的感知能力，有机会在生产者的消息无法被投递时发现并处理。但有时候，我们并不知道该如何处理这些无法路由的消息，最多打个日志，然后触发报警，再来手动处理。而通过日志来处理这些无法路由的消息是很不优雅的做法，特别是当生产者所在的服务有多台机器的时候，手动复制日志会更加麻烦而且容易出错。而且设置 mandatory 参数会增加生产者的复杂性，需要添加处理这些被退回的消息的逻辑。如果既不想丢失消息，又不想增加生产者的复杂性，该怎么做呢？前面在设置死信队列的文章中，我们提到，可以为队列设置死信交换机来存储那些处理失败的消息，可是这些不可路由消息根本没有机会进入到队列，因此无法使用死信队列来保存消息。在 RabbitMQ 中，有一种备份交换机的机制存在，可以很好的应对这个问题。什么是备份交换机呢？备份交换机可以理解为 RabbitMQ 中交换机的“备胎”，当我们为某一个交换机声明一个对应的备份交换机时，就是为它创建一个备胎，当交换机接收到一条不可路由消息时，将会把这条消息转发到备份交换机中，由备份交换机来进行转发和处理，通常备份交换机的类型为 Fanout，这样就能把所有消息都投递到与其绑定的队列中，然后我们在备份交换机下绑定一个队列，这样所有那些原交换机无法被路由的消息，就会都进入这个队列了。当然，我们还可以建立一个报警队列，用独立的消费者来进行监测和报警。

### 代码结构图

![image-20211022150114799](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211022150114799.png)

### 声明备份交换机和队列

```java
@Configuration
public class ConfirmConfig {
	public static final String CONFIRM_EXCHANGE_NAME = "confirm_exchange";
	public static final String CONFIRM_QUEUE_NAME = "confirm_queue";
	public static final String CONFIRM_ROUTING_KEY = "key1";

	//备份交换机
	public static final String BACKUP_EXCHANGE_NAME = "backup_exchange";
	//备份队列
	public static final String BACKUP_QUEUE_NAME = "backup_queue";
	//报警队列
	public static final String WARNING_QUEUE_NAME = "warning_queue";

	@Bean
	public Queue confirmQueue() {
		return QueueBuilder.durable(CONFIRM_QUEUE_NAME).build();
	}

	@Bean
	public DirectExchange confirmExchange() {
		return ExchangeBuilder.directExchange(CONFIRM_EXCHANGE_NAME).durable(true).withArgument("alternate-exchange", BACKUP_EXCHANGE_NAME).build();
	}

	@Bean
	public Binding queueBindingExchange(@Qualifier("confirmExchange") DirectExchange confirmExchange,
	                                    @Qualifier("confirmQueue") Queue confirmQueue) {
		return BindingBuilder.bind(confirmQueue).to(confirmExchange).with(CONFIRM_ROUTING_KEY);
	}

	//备份交换机
	@Bean
	public FanoutExchange backupExchange() {
		return new FanoutExchange(BACKUP_EXCHANGE_NAME);
	}

	@Bean
	public Queue backupQueue() {
		return QueueBuilder.durable(BACKUP_QUEUE_NAME).build();
	}

	@Bean
	public Queue warningQueue() {
		return QueueBuilder.durable(WARNING_QUEUE_NAME).build();
	}

	@Bean
	public Binding backupQueueBindingExchange(@Qualifier("backupExchange") FanoutExchange backupExchange,
	                                          @Qualifier("backupQueue") Queue backupQueue) {
		return BindingBuilder.bind(backupQueue).to(backupExchange);
	}

	@Bean
	public Binding warningQueueBindingExchange(@Qualifier("backupExchange") FanoutExchange backupExchange,
	                                           @Qualifier("warningQueue") Queue warningQueue) {
		return BindingBuilder.bind(warningQueue).to(backupExchange);
	}
}
```

### 结果分析

**调用生产者发送三个消息，第一个消息成功被处理。第二个消息因为交换机名称错误，成功被交换机确认回调接口处理。第三个消息 routingKey 错误，但消息已经到达交换机，被交换机放入设置的备份交换机，并且不触发交换机回退消息接口。**

回退消息和备份交换机，**备份交换机优先级更高**。

# 其它知识点

## 幂等性

### 概念

用户对于同一操作发起的一次请求或者多次请求的结果是一致的，不会因为多次点击而产生了副作用。举个最简单的例子，那就是支付，用户购买商品后支付，支付扣款成功，但是返回结果的时候网络异常，此时钱已经扣了，用户再次点击按钮，此时会进行第二次扣款，返回结果成功，用户查询余额发现多扣钱了，流水记录也变成了两条。在以前的单应用系统中，我们只需要把数据操作放入事务中即可，发生错误立即回滚，但是在响应客户端的时候也有可能出现网络中断或者异常等等。

### 消息重复消费

消费者在消费 MQ 中的消息时，MQ 已把消息发送给消费者，消费者在给 MQ 返回 ack 时网络中断，故 MQ 未收到确认信息，该条消息会重新发给其他的消费者，或者在网络重连后再次发送给该消费者，但实际上该消费者已成功消费了该条消息，造成消费者消费了重复的消息。

### 解决思路

MQ 消费者的幂等性的解决一般使用全局 ID 或者写个唯一标识比如时间戳或者 UUID 或者订单消费者消费 MQ 中的消息也可利用 MQ 的该 id 来判断，或者可按自己的规则生成一个全局唯一 id，每次消费消息时用该 id 先判断该消息是否已消费过。

### 消费端的幂等性保障

在海量订单生成的业务高峰期，生产端有可能就会重复发送了消息，这时候消费端就要实现幂等性，这就意味着我们的消息永远不会被消费多次，即使我们收到了一样的消息。业界主流的幂等性有两种操作：1.唯一 ID + 指纹码机制，利用数据库主键去重 2.利用 Redis 的原子性去实现

### 唯一 ID + 指纹码机制

指纹码：我们的一些规则或者时间戳加别的服务给到的唯一信息码，它并不一定是我们系统生成的，基本都是由我们的业务规则拼接而来，但是一定要保证唯一性，然后就利用查询语句进行判断这个 id 是否存在数据库中，优势就是实现简单就一个拼接，然后查询判断是否重复；劣势就是在高并发时，如果是单个数据库就会有写入性能瓶颈当然也可以采用分库分表提升性能，但也不是最推荐的方式。

### Redis原子性

利用 Redis 执行 setnx 命令，天然具有幂等性。

## 优先级队列

### 使用场景

在系统中有一个**订单催付**的场景，客户在天猫下的订单，淘宝会及时将订单推送给我们，如果在用户设定的时间内未付款那么就会给用户推送一条短信提醒，但是，天猫商家对我们来说，肯定是要分大客户和小客户的，比如像苹果，小米这样大商家一年起码能给我们创造很大的利润，所以理应当然，他们的订单必须得到优先处理，而曾经我们的后端系统是使用 Redis 来存放的定时轮询，大家都道 Redis 只能用 List 做一个简简单单的消息队列，并不能实现一个优先级的场景，所以订单量大了后采用 RabbitMQ 进行改造和优化，如果发现是大客户的订单给一个相对比较高的优先级，否则就是默认优先级。

### 实战

队列实现优先级需要做如下事情：队列需要设置为优先级队列，生产者发送消息需要设置消息的优先级，消费者需要等待消息已经发送到队列中才去消费，这样才有机会对消息进行排序

设置优先级队列：

```java
Map<String, Object> arguments = new HashMap<>();
//允许范围0-255，现在是0-10，设置过大浪费CPU与内存
arguments.put("x-max-priority", 10);
channel.queueDeclare(QUEUE_NAME, true, false, false, arguments);
```

消息设置优先级：

```java
AMQP.BasicProperties properties = new AMQP.BasicProperties().builder().priority(5).build();
channel.basicPublish("", QUEUE_NAME, properties, msg.getBytes());
```

生产者：

```java
public class Producer {
	public static final String QUEUE_NAME = "priority";

	public static void main(String[] args) throws IOException {
		Channel channel = RabbitMqUtils.getChannel();
		Map<String, Object> arguments = new HashMap<>();
		//允许范围0-255，现在是0-10，设置过大浪费CPU与内存
		arguments.put("x-max-priority", 10);
		channel.queueDeclare(QUEUE_NAME, true, false, false, arguments);

		for (int i = 0; i < 10; i++) {
			String msg = i + "";
			if (i == 5) {
				AMQP.BasicProperties properties = new AMQP.BasicProperties().builder().priority(5).build();
				channel.basicPublish("", QUEUE_NAME, properties, msg.getBytes());
			} else {
				channel.basicPublish("", QUEUE_NAME, null, msg.getBytes());
			}
		}
		System.out.println("消息发送完毕");
	}
}
```

消费者：

```java
public class Consumer {
	public static final String QUEUE_NAME = "priority";

	public static void main(String[] args) throws IOException {
		Channel channel = RabbitMqUtils.getChannel();
		DeliverCallback deliverCallback = (consumerTag, message) -> {
			System.out.println(new String(message.getBody()));
		};
		CancelCallback cancelCallback = (consumerTag) -> {
			System.out.println("消费消息被中断");
		};
		channel.basicConsume(QUEUE_NAME, true, deliverCallback, cancelCallback);
	}
}
```

**结果：i == 5 的消息最先被消费者消费。**

## 惰性队列

### 使用场景

RabbitMQ 从 3.6.0 版本开始引入了惰性队列的概念。惰性队列会尽可能的将消息存入磁盘中，而在消费者消费到相应的消息时才会被加载到内存中，它的一个重要的设计目标是能够支持更长的队列，即支持更多的消息存储。当消费者由于各种各样的原因（比如消费者下线、宕机亦或者是由于维护而关闭等）而致使长时间内不能消费消息造成堆积时，惰性队列就很有必要了。

默认情况下，当生产者将消息发送到 RabbitMQ 的时候，队列中的消息会尽可能的存储在内存之中，这样可以更加快速的将消息发送给消费者。即使是持久化的消息，在被写入磁盘的同时也会在内存中驻留一份备份。当 RabbitMQ 需要释放内存的时候，会将内存中的消息换页至磁盘中，这个操作会耗费较长的时间，也会阻塞队列的操作，进而无法接收新的消息。虽然 RabbitMQ 的开发者们一直在升级相关的算法，但是效果始终不太理想，尤其是在消息量特别大的时候。

### 两种模式

队列具备两种模式：default 和 lazy。默认的为 default 模式，在 3.6.0 之前的版本无需做任何变更。lazy 模式即为惰性队列的模式，可以通过调用`channel.queueDecare()`方法的时候在参数中设置，也可以通过 Policy 的方式设置，如果一个队列同时使用这两种方式设置的话，那么 Policy 的方式具备更高的优先级。如果要通过声明的方式改变已有队列的模式的话，那么只能先删除队列，然后再重新声明一个新的。

在队列声明的时候可以通过`x-queue-mode`参数来设置队列的模式，取值为`default`和`lazy`。下面示例中演示了一个惰性队列的声明细节：

```java
Map<String, Object> args = new HashMap<>();
args.put("x-queue-mode", "lazy");
channel.queueDeclare("myqueue", false, false, false, args);
```

### 内存开销对比

![image-20211022221247258](https://cdn.naccl.top/blog/blogHosting/2021/10/B01/image-20211022221247258.png)

在发送一百万条消息，每条消息大概占 1KB 的情况下，普通队列占用内存是 1.2GB，而惰性队列仅仅占用 1.5MB。