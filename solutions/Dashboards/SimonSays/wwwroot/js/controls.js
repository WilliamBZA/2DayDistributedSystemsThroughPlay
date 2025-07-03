document.addEventListener('DOMContentLoaded', function () {
    // Get anti-forgery token from the hidden input
    const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    async function buttonClicked(handler, data) {
        await fetch(`?handler=${handler}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': antiForgeryToken
            },
            body: JSON.stringify(data)
        });
    }

    document.querySelectorAll('button[data-handler]').forEach(button => {
        button.addEventListener('click', async function(e) {
            const handler = this.getAttribute('data-handler');
            if (handler === 'ChangeDifficulty') {
                const difficultyValue = document.getElementById('difficultyInput')?.value;
                buttonClicked(handler, { newDifficulty: Number(difficultyValue) });
            } else {
                buttonClicked(handler, {});
            }
        });
    });

    window.sendButtonClicked = (buttonNumber) => {
        buttonClicked("CaptureInput", { buttonNumber: buttonNumber });
    };
});