$(document).ready(function() {
    // Initialize date pickers if not already initialized by the template
    if ($.fn.datepicker && $('.datetimepicker').length > 0) {
        $('.datetimepicker').datepicker({
            format: 'd/m/y'
        });
    }
    
    // Toggle end date field based on "currently work here" checkbox
    $('#experience-current').on('change', function() {
        if ($(this).is(':checked')) {
            $('#experience-to').prop('disabled', true);
            $('#experience-to').val('');
        } else {
            $('#experience-to').prop('disabled', false);
        }
    });
    
    // Trigger initial state
    $('#experience-current').trigger('change');
    
    // Handle form submission
    $('#experience-form').on('submit', function(e) {
        e.preventDefault();
        saveExperience();
        return false;
    });
    
    // Connect submit event to the save button
    $('#save-experience').on('click', function() {
        saveExperience();
    });
    
    function saveExperience() {
        console.log("Save experience function called");
        
        // Get form data
        const company = $('#company').val();
        const position = $('#position').val();
        const city = $('#city').val();
        const description = $('#exp-description').val();
        const isCurrentJob = $('#experience-current').is(':checked');
        const fromDate = $('#experience-form2').val();
        const toDate = $('#experience-to').val();
        
        console.log("Form values:", { company, position, city, fromDate, toDate, isCurrentJob });
        
        // Basic validation
        if (!company || !position) {
            alert('Please fill in required fields (Company and Position)');
            return false;
        }
        
        if (!fromDate) {
            alert('Please enter a valid start date');
            return false;
        }
        
        if (!isCurrentJob && !toDate) {
            alert('Please enter an end date or check "I currently work here"');
            return false;
        }
        
        // Parse dates
        let startDate, endDate;
        try {
            // Convert from d/m/y format to a valid date
            const fromParts = fromDate.split('/');
            if (fromParts.length === 3) {
                // Adjust year if it's 2 digits
                let year = fromParts[2];
                if (year.length === 2) {
                    year = '20' + year; // Assuming 21st century
                }
                
                // Format date in ISO format (yyyy-MM-dd)
                startDate = `${year}-${fromParts[1].padStart(2, '0')}-${fromParts[0].padStart(2, '0')}`;
                console.log("Parsed start date:", startDate);
            } else {
                throw new Error('Invalid date format for start date');
            }
            
            if (!isCurrentJob && toDate) {
                const toParts = toDate.split('/');
                if (toParts.length === 3) {
                    // Adjust year if it's 2 digits
                    let year = toParts[2];
                    if (year.length === 2) {
                        year = '20' + year; // Assuming 21st century
                    }
                    
                    // Format date in ISO format (yyyy-MM-dd)
                    endDate = `${year}-${toParts[1].padStart(2, '0')}-${toParts[0].padStart(2, '0')}`;
                    console.log("Parsed end date:", endDate);
                } else {
                    throw new Error('Invalid date format for end date');
                }
            }
        } catch (error) {
            console.error('Date parsing error:', error);
            alert('Please enter valid dates in the format d/m/y');
            return false;
        }
        
        // Prepare data for submission
        const data = {
            Company: company,
            Position: position,
            City: city || '',
            Description: description || '',
            IsCurrentJob: isCurrentJob,
            StartDate: startDate,
            EndDate: isCurrentJob ? null : endDate
        };
        
        console.log("Sending data to server:", data);
        
        // Show loading state
        const $saveBtn = $('#save-experience');
        const originalText = $saveBtn.text();
        $saveBtn.text('Saving...').prop('disabled', true);
        
        // Submit the data
        $.ajax({
            url: '/Profile/SaveExperience',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            success: function(response) {
                console.log("Server response:", response);
                
                if (response.success) {
                    // Reset the form
                    $('#experience-form')[0].reset();
                    $('#experience-current').trigger('change');
                    
                    // Close the form
                    $('#experience-form1').collapse('hide');
                    
                    // Show success message
                    alert('Experience saved successfully!');
                    
                    // Reload the page to show new experience
                    window.location.reload();
                } else {
                    alert(response.message || 'Error saving experience');
                }
            },
            error: function(xhr, status, error) {
                console.error('AJAX error:', xhr.responseText);
                alert('Error saving experience. Please try again.');
            },
            complete: function() {
                // Restore button state
                $saveBtn.text(originalText).prop('disabled', false);
            }
        });
        
        return false;
    }
}); 