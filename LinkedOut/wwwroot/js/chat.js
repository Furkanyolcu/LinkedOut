// chat.js - Handle SignalR communication for the chat feature

// Initialize SignalR connection
const chatConnection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .build();

// Start the connection
chatConnection.start().catch(err => console.error("Error starting SignalR connection:", err.toString()));

// Handle receiving a message
chatConnection.on("ReceiveMessage", function (message) {
    // Check if chat with this sender is currently open
    const isActiveChatOpen = $("#active-chat-content").data("user-id") == message.senderId;
    
    if (isActiveChatOpen) {
        // Add message to the active chat
        const currentTime = new Date(message.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        const newMessage = `
            <div class="message received" data-message-id="${message.id}">
                <div class="message-content">${message.content}</div>
                <div class="message-time">${currentTime}</div>
            </div>
        `;
        $("#active-chat-content .chat-messages-content").append(newMessage);
        
        // Scroll to bottom
        const messageContainer = document.querySelector("#active-chat-content .chat-messages-content");
        messageContainer.scrollTop = messageContainer.scrollHeight;
        
        // Mark message as read
        chatConnection.invoke("MarkAsRead", message.id).catch(function (err) {
            console.error("Error marking message as read:", err.toString());
        });
    }
    
    // Update conversation list
    loadConversations();
});

// Handle message sent confirmation
chatConnection.on("MessageSent", function (message) {
    // The message object contains: id, receiverId, content, timestamp
    console.log("Message sent successfully", message);
});

// Handle message read confirmation
chatConnection.on("MessageRead", function (messageId) {
    // Update message status icon
    $(`.message[data-message-id=${messageId}] .message-status`).removeClass("sent").addClass("read");
    $(`.message[data-message-id=${messageId}] .message-status i`).removeClass("fa-check").addClass("fa-check-double");
});

// Load conversations list
function loadConversations() {
    $.ajax({
        url: '/Message/GetConversations',
        type: 'GET',
        success: function (data) {
            $("#chat-list").empty();
            
            if (data.length === 0) {
                $("#chat-list").html('<div class="p-3 text-center text-muted">No conversations yet</div>');
                return;
            }
            
            data.forEach(function (conversation) {
                const lastMessageTime = new Date(conversation.lastMessageTime).toLocaleDateString([], { 
                    day: '2-digit', month: '2-digit' 
                });
                
                let unreadBadge = '';
                if (conversation.unreadCount > 0) {
                    unreadBadge = `<div class="unread-badge">${conversation.unreadCount}</div>`;
                }
                
                const chatItem = `
                    <div class="chat-list-item p-3 border-bottom" data-user-id="${conversation.userId}">
                        <div class="d-flex justify-content-between align-items-center">
                            <div>
                                <h6 class="mb-1">${conversation.userName}</h6>
                                <p class="mb-0 text-truncate" style="max-width: 200px;">${conversation.lastMessage}</p>
                            </div>
                            <div class="d-flex flex-column align-items-end">
                                <small class="text-muted">${lastMessageTime}</small>
                                ${unreadBadge}
                            </div>
                        </div>
                    </div>
                `;
                
                $("#chat-list").append(chatItem);
            });
            
            // Attach click handlers
            $(".chat-list-item").click(function() {
                const userId = $(this).data("user-id");
                loadChat(userId);
                $(".chat-list-item").removeClass("active");
                $(this).addClass("active");
            });
        },
        error: function(error) {
            console.error("Error loading conversations:", error);
        }
    });
}

// Load chat with a specific user
function loadChat(userId) {
    $.ajax({
        url: `/Message/Chat/${userId}`,
        type: 'GET',
        success: function (data) {
            $("#chat-placeholder").removeClass("active");
            $("#active-chat-content").html(data).data("user-id", userId);
            
            // Notify the hub that we've joined this chat
            chatConnection.invoke("JoinChat", userId).catch(function (err) {
                console.error("Error joining chat:", err.toString());
            });
        },
        error: function(error) {
            console.error("Error loading chat:", error);
        }
    });
}

// Send a message via SignalR
function sendMessage(receiverId, content) {
    return chatConnection.invoke("SendMessage", receiverId, content).catch(function (err) {
        console.error("Error sending message:", err.toString());
        return false;
    });
}

// Document ready - initialize the chat functionality
$(document).ready(function() {
    // Load conversations when the page loads
    loadConversations();
    
    // Poll for new conversations every 30 seconds (as a backup to SignalR)
    setInterval(loadConversations, 30000);
    
    // Attach event delegation for the chat form submission
    $(document).on("submit", "[id^=chat-form-]", function(e) {
        e.preventDefault();
        
        const form = $(this);
        const receiverId = parseInt(form.find("input[name='receiverId']").val());
        const messageInput = form.find(".message-input");
        const content = messageInput.val().trim();
        
        if (content) {
            // Try sending via SignalR first
            sendMessage(receiverId, content)
                .then(function() {
                    // Clear the input after successful send
                    messageInput.val("");
                })
                .catch(function() {
                    // Fallback to AJAX if SignalR fails
                    $.ajax({
                        url: '/Message/Send',
                        type: 'POST',
                        data: {
                            receiverId: receiverId,
                            content: content
                        },
                        success: function(response) {
                            if (response.success) {
                                messageInput.val("");
                            }
                        }
                    });
                });
        }
    });
}); 