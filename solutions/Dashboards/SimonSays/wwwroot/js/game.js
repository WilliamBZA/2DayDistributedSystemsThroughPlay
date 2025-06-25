document.addEventListener('DOMContentLoaded', function () {
    const canvas = document.getElementById('game');
    const ctx = canvas.getContext('2d');
    const buttons = [];

    // Define button positions (4 rows x 3 columns)
    const buttonWidth = 80;
    const buttonHeight = 80;
    const paddingx = 80;
    const paddingy = 120;
    const startX = (canvas.width - (5.3 * (buttonWidth + paddingx))) / 2;
    const startY = (canvas.height + (0.6 * (buttonHeight + paddingy))) / 2;

    // Create buttons
    for (let row = 0; row < 2; row++) {
        for (let col = 0; col < 6; col++) {
            const buttonNumber = row * 3 + col;
            buttons.push({
                x: startX + col * (buttonWidth + paddingx),
                y: startY - row * (buttonHeight + paddingy),
                width: buttonWidth,
                height: buttonHeight,
                number: buttonNumber
            });
        }
    }

    // Draw buttons
    function drawButtons() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        buttons.forEach(button => {
            ctx.strokeStyle = 'black';
            ctx.lineWidth = 2;
            ctx.fillStyle = 'rgba(255, 255, 255, 0.1)';
            
            ctx.beginPath();
            ctx.rect(button.x, button.y, button.width, button.height);
            ctx.fill();
            ctx.stroke();
            
            // Add button number
            ctx.fillStyle = 'black';
            ctx.font = '20px Arial';
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillText((button.number + 1).toString(), 
                button.x + button.width / 2, 
                button.y + button.height / 2);
        });
    }

    // Handle click events
    canvas.addEventListener('click', function(event) {
        const rect = canvas.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;

        buttons.forEach(button => {
            if (x >= button.x && x <= button.x + button.width && y >= button.y && y <= button.y + button.height) {
                window.sendButtonClicked(button.number + 1);
            }
        });
    });

    // Initial draw
    drawButtons();
});