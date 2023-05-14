function convertToLocalTime() {
    const timeElements = document.getElementsByTagName('time');

    for (let timeElement of timeElements) {
        const dateTimeAttribute = timeElement.getAttribute('datetime');
        if (dateTimeAttribute === null) continue;

        const date = Date.parse(dateTimeAttribute);
        timeElement.textContent = new Intl.DateTimeFormat('default', {
            dateStyle: 'long',
            timeStyle: 'long'
        }).format(date);
    }
}

convertToLocalTime();
